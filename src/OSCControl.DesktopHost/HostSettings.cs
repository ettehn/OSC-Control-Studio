namespace OSCControl.DesktopHost;

internal sealed class HostSettings
{
    public string Language { get; set; } = HostLanguageMode.System;

    public string StartPage { get; set; } = HostStartPage.Script;

    public bool AutoRestoreLastFile { get; set; }

    public bool AutoCheckOnEdit { get; set; }

    public bool ClearRuntimeLogOnStart { get; set; } = true;

    public bool RememberWindowBounds { get; set; } = true;

    public bool DglabUnsafeRaw { get; set; }

    public string DglabSocketHost { get; set; } = "127.0.0.1";

    public int DglabSocketPort { get; set; } = 5678;

    public string DglabSocketPath { get; set; } = "/";

    public string? LastOpenedPath { get; set; }

    public int? WindowLeft { get; set; }

    public int? WindowTop { get; set; }

    public int? WindowWidth { get; set; }

    public int? WindowHeight { get; set; }
}

internal static class HostLanguageMode
{
    public const string System = "system";
    public const string English = "en";
    public const string SimplifiedChinese = "zh-Hans";
}

internal static class HostStartPage
{
    public const string Script = "script";
    public const string Blocks = "blocks";
    public const string DglabConnection = "dglab-connection";
    public const string Settings = "settings";
    public const string Diagnostics = "diagnostics";
    public const string Runtime = "runtime";
}
