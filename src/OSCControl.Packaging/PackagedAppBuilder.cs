using System.Text.Json;
using OSCControl.Compiler.Compiler;
using OSCControl.Compiler.Runtime;

namespace OSCControl.Packaging;

public sealed class PackagedAppBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public async Task<PackageBuildResult> BuildAsync(PackageBuildRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var outputRoot = Path.GetFullPath(request.OutputRoot);
        var appName = string.IsNullOrWhiteSpace(request.AppName) ? "OSCControl App" : request.AppName.Trim();
        var appFolderName = SanitizeDirectoryName(appName);
        var hostSource = string.IsNullOrWhiteSpace(request.HostSource) ? null : Path.GetFullPath(request.HostSource);

        if (hostSource is not null && !Directory.Exists(hostSource))
        {
            throw new DirectoryNotFoundException($"Host folder not found: {hostSource}");
        }

        var result = new CompilerPipeline().Compile(request.Source);
        if (result.HasErrors || result.Plan is null)
        {
            throw new PackageBuildException("Script has diagnostics and cannot be packaged.", result.Diagnostics);
        }

        var appRoot = Path.Combine(outputRoot, appFolderName);
        var appFolder = Path.Combine(appRoot, "app");
        var dataFolder = Path.Combine(appRoot, "data");
        var logsFolder = Path.Combine(appRoot, "logs");
        var hostFolder = Path.Combine(appRoot, "host");
        var assetsFolder = Path.Combine(appFolder, "assets");

        Directory.CreateDirectory(appFolder);
        Directory.CreateDirectory(dataFolder);
        Directory.CreateDirectory(logsFolder);
        Directory.CreateDirectory(hostFolder);
        Directory.CreateDirectory(assetsFolder);

        var manifest = new PackagedAppManifest
        {
            Name = appName,
            Script = "app.osccontrol",
            Plan = "app.plan.json",
            Data = "../data",
            Logs = "../logs"
        };

        var scriptOutputPath = Path.Combine(appFolder, manifest.Script);
        var manifestPath = Path.Combine(appFolder, "app.manifest.json");
        var planPath = Path.Combine(appFolder, "app.plan.json");
        var runCommandPath = Path.Combine(appRoot, "run.cmd");

        await File.WriteAllTextAsync(scriptOutputPath, request.Source, cancellationToken);
        await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(manifest, JsonOptions), cancellationToken);
        await File.WriteAllTextAsync(planPath, RuntimePlanJsonCodec.Serialize(result.Plan), cancellationToken);

        var hostCopied = false;
        if (hostSource is not null)
        {
            CopyDirectory(hostSource, hostFolder);
            hostCopied = true;
        }

        await File.WriteAllTextAsync(runCommandPath, BuildRunCommand(), cancellationToken);

        return new PackageBuildResult
        {
            AppRoot = appRoot,
            AppFolder = appFolder,
            ManifestPath = manifestPath,
            ScriptPath = scriptOutputPath,
            PlanPath = planPath,
            RunCommandPath = runCommandPath,
            HostCopied = hostCopied
        };
    }

    private static string BuildRunCommand() =>
        "@echo off\r\n" +
        "setlocal\r\n" +
        "cd /d %~dp0\r\n" +
        "if exist \"host\\OSCControl.AppHost.exe\" (\r\n" +
        "  \"host\\OSCControl.AppHost.exe\" app\r\n" +
        "  exit /b %ERRORLEVEL%\r\n" +
        ")\r\n" +
        "if exist \"host\\OSCControl.AppHost.dll\" (\r\n" +
        "  dotnet \"host\\OSCControl.AppHost.dll\" app\r\n" +
        "  exit /b %ERRORLEVEL%\r\n" +
        ")\r\n" +
        "echo Host files are missing. Copy OSCControl.AppHost into the host folder first.\r\n" +
        "exit /b 1\r\n";

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

    private static string SanitizeDirectoryName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = name.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray();
        var sanitized = new string(chars).Trim();
        return string.IsNullOrWhiteSpace(sanitized) ? "OSCControlApp" : sanitized;
    }
}