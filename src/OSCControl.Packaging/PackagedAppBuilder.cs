using System.Text;
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

    private static readonly HashSet<string> WindowsReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    public async Task<PackageBuildResult> BuildAsync(PackageBuildRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var outputRoot = Path.GetFullPath(request.OutputRoot);
        var appName = string.IsNullOrWhiteSpace(request.AppName) ? "OSCControl App" : request.AppName.Trim();
        var appFolderName = SanitizeDirectoryName(appName);
        var hostSource = string.IsNullOrWhiteSpace(request.HostSource) ? null : Path.GetFullPath(request.HostSource);
        var sourceScriptLabel = CreateSourceScriptLabel(request.ScriptPath);

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

        if (hostSource is not null && PathsOverlap(hostSource, hostFolder))
        {
            throw new InvalidOperationException($"Host source must not overlap the generated host folder: {hostSource}");
        }

        PreparePackageDirectory(appRoot, directoriesToClean: [appFolder, hostFolder], directoriesToCreate: [appFolder, dataFolder, logsFolder, hostFolder, assetsFolder]);

        var manifest = new PackagedAppManifest
        {
            Name = appName,
            Script = "app.osccontrol",
            Plan = "app.plan.json",
            Data = "../data",
            Logs = "../logs",
            SourceScript = sourceScriptLabel
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

    private static string? CreateSourceScriptLabel(string? scriptPath)
    {
        if (string.IsNullOrWhiteSpace(scriptPath))
        {
            return null;
        }

        var label = Path.GetFileName(scriptPath.Trim());
        return string.IsNullOrWhiteSpace(label) ? null : label;
    }

    private static void PreparePackageDirectory(string appRoot, IReadOnlyList<string> directoriesToClean, IReadOnlyList<string> directoriesToCreate)
    {
        Directory.CreateDirectory(appRoot);

        foreach (var directory in directoriesToClean)
        {
            EnsureInsidePackageRoot(directory, appRoot);
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        foreach (var directory in directoriesToCreate)
        {
            EnsureInsidePackageRoot(directory, appRoot);
            Directory.CreateDirectory(directory);
        }
    }

    private static void EnsureInsidePackageRoot(string path, string appRoot)
    {
        if (!IsPathInside(path, appRoot))
        {
            throw new InvalidOperationException($"Package directory escapes app root: {path}");
        }
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
        var builder = new StringBuilder(name.Length);
        var lastWasUnderscore = false;
        foreach (var character in name)
        {
            var safe = invalid.Contains(character) || char.IsControl(character) ? '_' : character;
            if (safe == '_')
            {
                if (lastWasUnderscore)
                {
                    continue;
                }

                lastWasUnderscore = true;
            }
            else
            {
                lastWasUnderscore = false;
            }

            builder.Append(safe);
        }

        var sanitized = builder.ToString().Trim().TrimEnd('.', ' ');
        if (string.IsNullOrWhiteSpace(sanitized))
        {
            sanitized = "OSCControlApp";
        }

        var baseName = sanitized.Split('.')[0];
        if (WindowsReservedNames.Contains(baseName))
        {
            sanitized = $"{sanitized}_app";
        }

        return sanitized;
    }

    private static bool PathsOverlap(string left, string right) =>
        IsPathInsideOrEqual(left, right) || IsPathInsideOrEqual(right, left);

    private static bool IsPathInside(string path, string root)
    {
        var fullPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase);
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
