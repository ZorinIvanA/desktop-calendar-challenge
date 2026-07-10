using DesktopCalendar.Core.Contracts;

namespace DesktopCalendar.Platform.Linux;

/// <summary>
/// Linux-реализация IMonitorService. В M1 — заглушка.
/// В M2: через Avalonia Gdk.Display / xrandr; стабильный id = connector name.
/// </summary>
public sealed class LinuxMonitorService : IMonitorService
{
    public IReadOnlyList<MonitorInfo> GetMonitors()
    {
        // TODO M2: Gdk.DisplayManager + Monitor.Connector.
        throw new NotImplementedException("LinuxMonitorService реализуется в M2.");
    }

    public MonitorInfo? GetById(string id)
    {
        // TODO M2.
        throw new NotImplementedException("LinuxMonitorService реализуется в M2.");
    }
}
