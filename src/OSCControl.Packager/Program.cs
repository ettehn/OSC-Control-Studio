using System.Text.Json;
using OSCControl.Compiler.Compiler;

namespace OSCControl.Packager;

internal static class Program
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static async Task<int> Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Usage: OSCControl.Packager <script.osccontrol> <output-folder> [app-name] [host-folder]");
            return 2;
        }

        var scriptPath = Path.GetFullPath(args[0]);
        var outputRoot = Path.GetFullPath(args[1]);
        var appName = args.Length > 2 ? args[2] : Path.GetFileNameWithoutExtension(scriptPath);
        var hostSource = args.Length > 3 ? Path.GetFullPath(args[3]) : null;

        if (!File.Exists(scriptPath))
        {
            Console.Error.WriteLine($"Script not found: {scriptPath}");
            return 2;
        }

        if (hostSource is not null && !Directory.Exists(hostSource))
        {
            Console.Error.WriteLine($"Host folder not found: {hostSource}");
            return 2;
        }

        var source = await File.ReadAllTextAsync(scriptPath);
        var result = new CompilerPipeline().Compile(source);
        if (result.HasErrors || result.Plan is null)
        {
            foreach (var diagnostic in result.Diagnostics)
            {
                Console.Error.WriteLine($"{diagnostic.Severity} {diagnostic.Span.Start.Line}:{diagnostic.Span.Start.Column}: {diagnostic.Message}");
            }

            return 1;
        }

        var appRoot = Path.Combine(outputRoot, appName);
        var appFolder = Path.Combine(appRoot, "app");
        var dataFolder = Path.Combine(appRoot, "data");
        var logsFolder = Path.Combine(appRoot, "logs");
        var hostFolder = Path.Combine(appRoot, "host");
        Directory.CreateDirectory(appFolder);
        Directory.CreateDirectory(dataFolder);
        Directory.CreateDirectory(logsFolder);
        Directory.CreateDirectory(hostFolder);

        var manifest = new PackagedAppManifest
        {
            Name = appName,
            Script = "app.osccontrol",
            Plan = "app.plan.json",
            Logs = "../logs"
        };

        await File.WriteAllTextAsync(Path.Combine(appFolder, manifest.Script), source);
        await File.WriteAllTextAsync(Path.Combine(appFolder, "app.manifest.json"), JsonSerializer.Serialize(manifest, JsonOptions));

        var planPreview = new
        {
            note = "RuntimePlan JSON serialization is not yet authoritative. AppHost recompiles app.osccontrol at startup for now.",
            endpoints = result.Plan.Endpoints.Count,
            states = result.Plan.States.Count,
            rules = result.Plan.Rules.Count
        };
        await File.WriteAllTextAsync(Path.Combine(appFolder, "app.plan.json"), JsonSerializer.Serialize(planPreview, JsonOptions));

        if (hostSource is not null)
        {
            CopyDirectory(hostSource, hostFolder);
        }

        await File.WriteAllTextAsync(Path.Combine(appRoot, "run.cmd"), BuildRunCommand(hostSource is not null));

        Console.WriteLine($"Packaged app: {appRoot}");
        Console.WriteLine($"Manifest: {Path.Combine(appFolder, "app.manifest.json")}");
        Console.WriteLine(hostSource is null
            ? "Host folder was not provided. Publish/copy OSCControl.AppHost into the host folder before distribution."
            : $"Host copied from: {hostSource}");
        return 0;
    }

    private static string BuildRunCommand(bool hasHost)
    {
        if (hasHost)
        {
            return "@echo off\r\nsetlocal\r\ncd /d %~dp0\r\ndotnet host\\OSCControl.AppHost.dll app\r\n";
        }

        return "@echo off\r\necho Host files are missing. Copy OSCControl.AppHost into the host folder first.\r\nexit /b 1\r\n";
    }

    private static void CopyDirectory(string sourceDirectory, string targetDirectory)
    {
        Directory.CreateDirectory(targetDirectory);

        foreach (var file in Directory.EnumerateFiles(sourceDirectory))
        {
            var targetFile = Path.Combine(targetDirectory, Path.GetFileName(file));
            File.Copy(file, targetFile, overwrite: true);
        }

        foreach (var directory in Directory.EnumerateDirectories(sourceDirectory))
        {
            var targetChild = Path.Combine(targetDirectory, Path.GetFileName(directory));
            CopyDirectory(directory, targetChild);
        }
    }
}