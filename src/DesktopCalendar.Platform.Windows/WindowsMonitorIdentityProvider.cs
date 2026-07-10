using System.Runtime.Versioning;
using DesktopCalendar.Core.Platform;

namespace DesktopCalendar.Platform.Windows;

/// <summary>
/// Windows-источник стабильных идентификаторов мониторов через COM IDesktopWallpaper.
/// Device path из GetMonitorDevicePathAt — тот же ключ, что использует SetWallpaper в M4.
/// Геометрия здесь не нужна (она из Avalonia) — передаём только bounds для сшивки в MonitorService.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsMonitorIdentityProvider : IMonitorIdentityProvider
{
    public IReadOnlyList<MonitorIdentity> GetIdentities()
    {
        if (!OperatingSystem.IsWindows())
        {
            return Array.Empty<MonitorIdentity>();
        }

        var wallpaper = (IDesktopWallpaper)Activator.CreateInstance(Type.GetTypeFromCLSID(DesktopWallpaperClsid.Value)!)!;
        try
        {
            var count = wallpaper.GetMonitorDevicePathCount();
            var result = new List<MonitorIdentity>((int)count);
            for (uint i = 0; i < count; i++)
            {
                var devicePath = wallpaper.GetMonitorDevicePathAt(i);
                var rect = wallpaper.GetMonitorRECT(devicePath);
                result.Add(new MonitorIdentity(
                    Id: devicePath,
                    Connector: ExtractConnector(devicePath),
                    DisplayName: null,
                    LogicalIndex: (int)(i + 1),
                    Bounds: new Core.Platform.ScreenRect(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top)));
            }
            return result;
        }
        finally
        {
            System.Runtime.InteropServices.Marshal.ReleaseComObject(wallpaper);
        }
    }

    /// <summary>Из device path вида \\?\DISPLAY#...#... извлечь короткое имя для UI.</summary>
    private static string? ExtractConnector(string devicePath)
        => string.IsNullOrWhiteSpace(devicePath) ? null : devicePath;
}
