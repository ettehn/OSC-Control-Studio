using System.Globalization;

namespace OSCControl.DesktopHost;

internal static class DesktopLocalization
{
    private static readonly IReadOnlyDictionary<string, string> SimplifiedChinese = new Dictionary<string, string>(StringComparer.Ordinal);

    public static bool UseSimplifiedChinese()
    {
        var language = HostSettingsStore.Load().Language;
        if (string.Equals(language, HostLanguageMode.SimplifiedChinese, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(language, HostLanguageMode.English, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var culture = CultureInfo.CurrentUICulture;
        return string.Equals(culture.TwoLetterISOLanguageName, "zh", StringComparison.OrdinalIgnoreCase);
    }

    public static string Translate(string english, string fallback)
    {
        if (SimplifiedChinese.TryGetValue(english, out var translated))
        {
            return translated;
        }

        return LooksCorrupted(fallback) ? english : fallback;
    }

    private static bool LooksCorrupted(string text) => text.Contains('\uFFFD') || text.Contains('\u951F') || text.Count(static ch => ch == '?') >= 3;
}
