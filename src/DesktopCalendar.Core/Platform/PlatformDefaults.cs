namespace DesktopCalendar.Core.Platform;

/// <summary>
/// Платформо-специфичные дефолты. Шрифт по умолчанию: Segoe UI на Windows, DejaVu Sans на Linux
/// (Noto Sans как альтернатива). Запрошенный шрифт может отсутствовать в системе —
/// CalendarRenderer тогда fallback'ает (M3); здесь только стартовое значение для свежих настроек.
/// </summary>
public static class PlatformDefaults
{
    /// <summary>Шрифт по умолчанию для текущей ОС.</summary>
    public static string DefaultFontFamily =>
        OperatingSystem.IsWindows() ? "Segoe UI"
        : OperatingSystem.IsLinux() ? "DejaVu Sans"
        : "Sans";
}
