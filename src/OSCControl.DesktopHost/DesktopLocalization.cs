using System.Globalization;

namespace OSCControl.DesktopHost;

internal static class DesktopLocalization
{
    public static bool UseSimplifiedChinese()
    {
        var culture = CultureInfo.CurrentUICulture;
        return string.Equals(culture.TwoLetterISOLanguageName, "zh", StringComparison.OrdinalIgnoreCase);
    }
}
