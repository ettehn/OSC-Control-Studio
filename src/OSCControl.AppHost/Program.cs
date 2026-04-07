using System.Text.Json;
using OSCControl.Compiler.Compiler;
using OSCControl.Compiler.Runtime;
using OSCControl.Packaging;

namespace OSCControl.AppHost;

internal static class Program
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static async Task<int> Main(string[] args)
    {
        var appRoot = ResolveAppRoot(args);
        if (appRoot is null)
        {
            var baseDirectory = AppContext.BaseDirectory;
            Console.Error.WriteLine("Manifest not found.");
            Console.Error.WriteLine($"Checked: {Path.Combine(baseDirectory, "app.manifest.json")}");
            Console.Error.WriteLine($"Checked: {Path.Combine(baseDirectory, "app", "app.manifest.json")}");
            Console.Error.WriteLine($"Checked: {Path.Combine(Path.GetFullPath(Path.Combine(baseDirectory, "..")), "app", "app.manifest.json")}");
            Console.Error.WriteLine("Usage: OSCControl.AppHost <packaged-app-folder>");
            return 2;
        }

        try
        {
            var manifestPath = Path.Combine(appRoot, "app.manifest.json");
            var manifest = JsonSerializer.Deserialize<PackagedAppManifest>(await File.ReadAllTextAsync(manifestPath), JsonOptions) ?? new PackagedAppManifest();
            var packageRoot = ResolvePackageRoot(appRoot);
            var scriptPath = ResolveManifestPath(appRoot, manifest.Script, appRoot, "script");
            var dataPath = ResolveManifestPath(appRoot, manifest.Data, packageRoot, "data");
            var logsPath = ResolveManifestPath(appRoot, manifest.Logs, packageRoot, "logs");

            Directory.CreateDirectory(dataPath);
            Directory.CreateDirectory(logsPath);

            var plan = await LoadPlanAsync(appRoot, manifest, scriptPath);
            if (plan is null)
            {
                return 1;
            }

            await using var engine = new RuntimeEngine(plan, new RuntimeEngineOptions
            {
                LogSink = new ConsoleRuntimeLogSink()
            });

            await using var host = new RuntimeHost(engine, new RuntimeHostOptions
            {
                ErrorSink = new ConsoleRuntimeHostErrorSink()
            });

            using var shutdown = new CancellationTokenSource();
            Console.CancelKeyPress += (_, eventArgs) =>
            {
                eventArgs.Cancel = true;
                shutdown.Cancel();
            };

            Console.WriteLine($"Running {manifest.Name}");
            Console.WriteLine($"App root: {appRoot}");
            Console.WriteLine("Press Ctrl+C to stop.");

            await host.StartAsync(shutdown.Token);

            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, shutdown.Token);
            }
            catch (OperationCanceledException)
            {
            }

            await host.StopAsync();
            return 0;
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 2;
        }
    }

    private static string? ResolveAppRoot(string[] args)
    {
        if (args.Length > 0)
        {
            var explicitRoot = Path.GetFullPath(args[0]);
            return File.Exists(Path.Combine(explicitRoot, "app.manifest.json")) ? explicitRoot : null;
        }

        var baseDirectory = AppContext.BaseDirectory;
        var candidates = new[]
        {
            baseDirectory,
            Path.Combine(baseDirectory, "app"),
            Path.Combine(Path.GetFullPath(Path.Combine(baseDirectory, "..")), "app")
        };

        return candidates.FirstOrDefault(candidate => File.Exists(Path.Combine(candidate, "app.manifest.json")));
    }

    private static string ResolvePackageRoot(string appRoot)
    {
        var appDirectory = Path.GetFileName(Path.GetFullPath(appRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        return string.Equals(appDirectory, "app", StringComparison.OrdinalIgnoreCase)
            ? Path.GetFullPath(Path.Combine(appRoot, ".."))
            : appRoot;
    }

    private static async Task<RuntimePlan?> LoadPlanAsync(string appRoot, PackagedAppManifest manifest, string scriptPath)
    {
        if (!string.IsNullOrWhiteSpace(manifest.Plan))
        {
            var planPath = ResolveManifestPath(appRoot, manifest.Plan, appRoot, "plan");
            if (File.Exists(planPath))
            {
                try
                {
                    var plan = RuntimePlanJsonCodec.Deserialize(await File.ReadAllTextAsync(planPath));
                    Console.WriteLine($"Loaded runtime plan: {planPath}");
                    return plan;
                }
                catch (Exception ex) when (ex is JsonException or InvalidOperationException or NotSupportedException)
                {
                    Console.Error.WriteLine($"Could not load runtime plan '{planPath}': {ex.Message}");
                    Console.Error.WriteLine("Falling back to app.osccontrol compilation.");
                }
            }
        }

        if (!File.Exists(scriptPath))
        {
            Console.Error.WriteLine($"Script not found: {scriptPath}");
            return null;
        }

        var source = await File.ReadAllTextAsync(scriptPath);
        var result = new CompilerPipeline().Compile(source);
        if (!result.HasErrors && result.Plan is not null)
        {
            Console.WriteLine($"Compiled runtime plan from script: {scriptPath}");
            return result.Plan;
        }

        foreach (var diagnostic in result.Diagnostics)
        {
            Console.Error.WriteLine($"{diagnostic.Severity} {diagnostic.Span.Start.Line}:{diagnostic.Span.Start.Column}: {diagnostic.Message}");
        }

        return null;
    }

    private static string ResolveManifestPath(string appRoot, string? relativePath, string allowedRoot, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new InvalidOperationException($"Manifest field '{fieldName}' is required.");
        }

        if (Path.IsPathFullyQualified(relativePath))
        {
            throw new InvalidOperationException($"Manifest field '{fieldName}' must be relative: {relativePath}");
        }

        var resolved = Path.GetFullPath(Path.Combine(appRoot, relativePath));
        if (!IsPathInsideOrEqual(resolved, allowedRoot))
        {
            throw new InvalidOperationException($"Manifest field '{fieldName}' escapes its allowed root: {relativePath}");
        }

        return resolved;
    }

    private static bool IsPathInsideOrEqual(string path, string root)
    {
        var fullPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return fullPath.Equals(fullRoot, StringComparison.OrdinalIgnoreCase)
            || fullPath.StartsWith(fullRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || fullPath.StartsWith(fullRoot + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }
}
