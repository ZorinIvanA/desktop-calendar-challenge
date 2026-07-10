namespace DesktopCalendar.Platform.Linux;

/// <summary>
/// Определяет текущее окружение рабочего стола по XDG_CURRENT_DESKTOP.
/// В M1 используется только для диагностики; в M4 — для выбора backend'а IWallpaperService.
/// </summary>
public enum LinuxDesktop { Unknown, Gnome, Kde, Xfce, Cinnamon, Mate }

public static class DesktopEnvironment
{
    /// <summary>
    /// Значение XDG_CURRENT_DESKTOP разбирается по ':', берётся первая компонента,
    /// нормализуется в нижний регистр. 'ubuntu' трактуется как GNOME (Ubuntu использует GNOME).
    /// </summary>
    public static LinuxDesktop Detect()
    {
        var raw = Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP") ?? string.Empty;
        var first = raw.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                       .FirstOrDefault()?
                       .ToLowerInvariant() ?? string.Empty;

        return first switch
        {
            "gnome" or "ubuntu" => LinuxDesktop.Gnome,
            "kde" => LinuxDesktop.Kde,
            "xfce" => LinuxDesktop.Xfce,
            "x-cinnamon" or "cinnamon" => LinuxDesktop.Cinnamon,
            "mate" => LinuxDesktop.Mate,
            _ => LinuxDesktop.Unknown,
        };
    }

    /// <summary>Человекочитаемое имя для шапки UI.</summary>
    public static string DisplayName(LinuxDesktop de) => de switch
    {
        LinuxDesktop.Gnome => "GNOME",
        LinuxDesktop.Kde => "KDE Plasma",
        LinuxDesktop.Xfce => "XFCE",
        LinuxDesktop.Cinnamon => "Cinnamon",
        LinuxDesktop.Mate => "MATE",
        _ => "Unknown DE",
    };
}
