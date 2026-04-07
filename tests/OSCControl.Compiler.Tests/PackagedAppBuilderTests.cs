using System.Text.Json;
using OSCControl.Compiler.Runtime;
using OSCControl.Packaging;
using Xunit;

namespace OSCControl.Compiler.Tests;

public sealed class PackagedAppBuilderTests
{
    [Fact]
    public async Task BuildAsync_WritesManifestScriptPlanAndCleansAppArtifacts()
    {
        var root = CreateTempDirectory();
        try
        {
            var builder = new PackagedAppBuilder();
            var source = """
on startup [
    log info "packaged"
]
""";

            var first = await builder.BuildAsync(new PackageBuildRequest
            {
                Source = source,
                ScriptPath = Path.Combine(root, "source.osccontrol"),
                OutputRoot = root,
                AppName = "CON."
            });

            Assert.EndsWith("CON_app", first.AppRoot, StringComparison.OrdinalIgnoreCase);
            Assert.True(File.Exists(first.ManifestPath));
            Assert.True(File.Exists(first.ScriptPath));
            Assert.True(File.Exists(first.PlanPath));
            Assert.True(File.Exists(first.RunCommandPath));

            var manifest = JsonSerializer.Deserialize<PackagedAppManifest>(await File.ReadAllTextAsync(first.ManifestPath), new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            Assert.NotNull(manifest);
            Assert.Equal("source.osccontrol", manifest.SourceScript);

            var plan = RuntimePlanJsonCodec.Deserialize(await File.ReadAllTextAsync(first.PlanPath));
            Assert.Single(plan.Rules);

            var staleAppFile = Path.Combine(first.AppFolder, "stale.txt");
            var staleHostFile = Path.Combine(first.AppRoot, "host", "stale.txt");
            var preservedDataFile = Path.Combine(first.AppRoot, "data", "keep.txt");
            await File.WriteAllTextAsync(staleAppFile, "stale");
            await File.WriteAllTextAsync(staleHostFile, "stale");
            await File.WriteAllTextAsync(preservedDataFile, "keep");

            var second = await builder.BuildAsync(new PackageBuildRequest
            {
                Source = source,
                OutputRoot = root,
                AppName = "CON."
            });

            Assert.False(File.Exists(Path.Combine(second.AppFolder, "stale.txt")));
            Assert.False(File.Exists(Path.Combine(second.AppRoot, "host", "stale.txt")));
            Assert.True(File.Exists(Path.Combine(second.AppRoot, "data", "keep.txt")));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }


    [Fact]
    public async Task BuildAsync_RejectsHostSourceInsideTargetHostFolder()
    {
        var root = CreateTempDirectory();
        try
        {
            var targetHost = Path.Combine(root, "App", "host");
            Directory.CreateDirectory(targetHost);

            var failed = false;
            try
            {
                await new PackagedAppBuilder().BuildAsync(new PackageBuildRequest
                {
                    Source = """
on startup [
    log info "packaged"
]
""",
                    OutputRoot = root,
                    AppName = "App",
                    HostSource = targetHost
                });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Host source", StringComparison.OrdinalIgnoreCase))
            {
                failed = true;
            }

            Assert.True(failed, "Expected overlapping HostSource to be rejected.");
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "OSCControl.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
