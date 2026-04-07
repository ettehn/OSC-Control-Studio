using OSCControl.Packaging;

namespace OSCControl.Packager;

internal static class Program
{
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

        try
        {
            var source = await File.ReadAllTextAsync(scriptPath);
            var result = await new PackagedAppBuilder().BuildAsync(new PackageBuildRequest
            {
                Source = source,
                ScriptPath = scriptPath,
                OutputRoot = outputRoot,
                AppName = appName,
                HostSource = hostSource
            });

            Console.WriteLine($"Packaged app: {result.AppRoot}");
            Console.WriteLine($"Manifest: {result.ManifestPath}");
            Console.WriteLine(result.HostCopied
                ? $"Host copied from: {hostSource}"
                : "Host folder was not provided. Publish/copy OSCControl.AppHost into the host folder before distribution.");
            return 0;
        }
        catch (PackageBuildException ex)
        {
            foreach (var diagnostic in ex.Diagnostics)
            {
                Console.Error.WriteLine($"{diagnostic.Severity} {diagnostic.Span.Start.Line}:{diagnostic.Span.Start.Column}: {diagnostic.Message}");
            }

            return 1;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or DirectoryNotFoundException)
        {
            Console.Error.WriteLine(ex.Message);
            return 2;
        }
    }
}