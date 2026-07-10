using DesktopCalendar.Core.Contracts;

namespace DesktopCalendar.Platform.Windows;

/// <summary>
/// Windows-реализация через COM IDesktopWallpaper. Полная имплементация — в M2.
/// В M1 это заглушка, чтобы DI-композиция компилировалась и работала "скелетно".
/// </summary>
public sealed class WindowsMonitorService : IMonitorService
{
    public IReadOnlyList<MonitorInfo> GetMonitors()
    {
        // TODO M2: IDesktopWallpaper.GetMonitorDevicePathCount/At + GetMonitorRECT.
        throw new NotImplementedException("WindowsMonitorService реализуется в M2.");
    }

    public MonitorInfo? GetById(string id)
    {
        // TODO M2.
        throw new NotImplementedException("WindowsMonitorService реализуется в M2.");
    }
}
