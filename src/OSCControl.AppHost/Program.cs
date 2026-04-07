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

        var manifestPath = Path.Combine(appRoot, "app.manifest.json");
        var manifest = JsonSerializer.Deserialize<PackagedAppManifest>(await File.ReadAllTextAsync(manifestPath), JsonOptions) ?? new PackagedAppManifest();
        var scriptPath = Path.Combine(appRoot, manifest.Script);

        Directory.CreateDirectory(Path.GetFullPath(Path.Combine(appRoot, manifest.Data)));
        Directory.CreateDirectory(Path.GetFullPath(Path.Combine(appRoot, manifest.Logs)));

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

    private static async Task<RuntimePlan?> LoadPlanAsync(string appRoot, PackagedAppManifest manifest, string scriptPath)
    {
        if (!string.IsNullOrWhiteSpace(manifest.Plan))
        {
            var planPath = Path.Combine(appRoot, manifest.Plan);
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
}