using System.Text.Json;

namespace OSCControl.DesktopHost;

internal static class HostSettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private static HostSettings? _current;

    public static HostSettings Load()
    {
        if (_current is not null)
        {
            return Clone(_current);
        }

        var loaded = TryLoadFromDisk() ?? new HostSettings();
        Normalize(loaded);
        _current = Clone(loaded);
        return loaded;
    }

    public static void Save(HostSettings settings)
    {
        Normalize(settings);
        var path = GetSettingsPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(settings, SerializerOptions));
        _current = Clone(settings);
    }

    private static HostSettings? TryLoadFromDisk()
    {
        var path = GetSettingsPath();
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<HostSettings>(File.ReadAllText(path), SerializerOptions);
        }
        catch
        {
            return new HostSettings();
        }
    }

    private static string GetSettingsPath()
    {
        var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OSCControl.DesktopHost");
        return Path.Combine(directory, "host-settings.json");
    }

    private static HostSettings Clone(HostSettings settings) => new()
    {
        Language = settings.Language,
        StartPage = settings.StartPage,
        AutoRestoreLastFile = settings.AutoRestoreLastFile,
        AutoCheckOnEdit = settings.AutoCheckOnEdit,
        ClearRuntimeLogOnStart = settings.ClearRuntimeLogOnStart,
        RememberWindowBounds = settings.RememberWindowBounds,
        DglabUnsafeRaw = settings.DglabUnsafeRaw,
        DglabSocketHost = settings.DglabSocketHost,
        DglabSocketPort = settings.DglabSocketPort,
        DglabSocketPath = settings.DglabSocketPath,
        LastOpenedPath = settings.LastOpenedPath,
        WindowLeft = settings.WindowLeft,
        WindowTop = settings.WindowTop,
        WindowWidth = settings.WindowWidth,
        WindowHeight = settings.WindowHeight
    };

    private static void Normalize(HostSettings settings)
    {
        settings.Language = settings.Language switch
        {
            HostLanguageMode.English => HostLanguageMode.English,
            HostLanguageMode.SimplifiedChinese => HostLanguageMode.SimplifiedChinese,
            _ => HostLanguageMode.System
        };

        settings.StartPage = settings.StartPage switch
        {
            HostStartPage.Blocks => HostStartPage.Blocks,
            HostStartPage.DglabConnection => HostStartPage.DglabConnection,
            HostStartPage.Settings => HostStartPage.Settings,
            HostStartPage.Diagnostics => HostStartPage.Diagnostics,
            HostStartPage.Runtime => HostStartPage.Runtime,
            _ => HostStartPage.Script
        };

        settings.DglabSocketHost = string.IsNullOrWhiteSpace(settings.DglabSocketHost) ? "127.0.0.1" : settings.DglabSocketHost.Trim();
        settings.DglabSocketPort = settings.DglabSocketPort is >= 1 and <= 65535 ? settings.DglabSocketPort : 5678;
        settings.DglabSocketPath = string.IsNullOrWhiteSpace(settings.DglabSocketPath) ? "/" : settings.DglabSocketPath.Trim();
        if (!settings.DglabSocketPath.StartsWith("/", StringComparison.Ordinal))
        {
            settings.DglabSocketPath = "/" + settings.DglabSocketPath;
        }
    }
}

