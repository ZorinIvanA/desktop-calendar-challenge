namespace DesktopCalendar.Core.Platform;

/// <summary>
/// Платформенный источник стабильных идентификаторов мониторов.
/// Реализация в DesktopCalendar.Platform.Windows (IDesktopWallpaper) и .Linux (Mutter D-Bus).
/// </summary>
public interface IMonitorIdentityProvider
{
    /// <summary>Идентификация всех подключённых мониторов в порядке, ожидаемом пользователем.</summary>
    IReadOnlyList<MonitorIdentity> GetIdentities();
}
