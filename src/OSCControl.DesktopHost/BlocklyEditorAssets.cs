namespace OSCControl.DesktopHost;

internal static class BlocklyEditorAssets
{
    public const string RootRelativePath = "BlocklyAssets";

    public static string GetIndexPath()
    {
        return Path.Combine(AppContext.BaseDirectory, RootRelativePath, "index.html");
    }

    public static bool IsAvailable()
    {
        return File.Exists(GetIndexPath());
    }
}