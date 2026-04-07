using System.Text.Json;
using OSCControl.Compiler.Compiler;
using OSCControl.Compiler.Runtime;

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
        var appRoot = args.Length > 0 ? Path.GetFullPath(args[0]) : AppContext.BaseDirectory;
        var manifestPath = Path.Combine(appRoot, "app.manifest.json");
        if (!File.Exists(manifestPath))
        {
            Console.Error.WriteLine($"Manifest not found: {manifestPath}");
            Console.Error.WriteLine("Usage: OSCControl.AppHost <packaged-app-folder>");
            return 2;
        }

        var manifest = JsonSerializer.Deserialize<AppManifest>(await File.ReadAllTextAsync(manifestPath), JsonOptions) ?? new AppManifest();
        var scriptPath = Path.Combine(appRoot, manifest.Script);
        if (!File.Exists(scriptPath))
        {
            Console.Error.WriteLine($"Script not found: {scriptPath}");
            return 2;
        }

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

    private static async Task<RuntimePlan?> LoadPlanAsync(string appRoot, AppManifest manifest, string scriptPath)
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