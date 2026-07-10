namespace DesktopCalendar.Core.Contracts;

/// <summary>
/// Доступ к списку мониторов системы. Реализация платформенная (Win / Linux).
/// </summary>
public interface IMonitorService
{
    /// <summary>Все мониторы в порядке и с номерами, как их видит ОС.</summary>
    IReadOnlyList<MonitorInfo> GetMonitors();

    /// <summary>Найти монитор по стабильному идентификатору или null.</summary>
    MonitorInfo? GetById(string id);
}
