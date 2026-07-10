using DesktopCalendar.Core.Contracts;

namespace DesktopCalendar.Platform.Windows;

/// <summary>
/// Windows-реализация IWallpaperService через IDesktopWallpaper (per-monitor).
/// Полная имплементация — в M4.
/// </summary>
public sealed class WindowsWallpaperService : IWallpaperService
{
    public WallpaperSnapshot GetCurrent(string monitorId)
    {
        // TODO M4: IDesktopWallpaper.GetWallpaper(monitorId).
        throw new NotImplementedException("WindowsWallpaperService реализуется в M4.");
    }

    public void SetWallpaper(string monitorId, string imageFilePath)
    {
        // TODO M4: IDesktopWallpaper.SetWallpaper(monitorId, path).
        throw new NotImplementedException("WindowsWallpaperService реализуется в M4.");
    }

    public string? GetOriginalPath(string monitorId)
    {
        // TODO M4: читать из OriginalsDir/<monitorId>.<ext> через IPlatformPaths.
        throw new NotImplementedException("WindowsWallpaperService реализуется в M4.");
    }

    public WallpaperFit ParseCurrentFit(string monitorId)
    {
        // TODO M4: IDesktopWallpaper.GetPosition → маппинг в WallpaperFit.
        throw new NotImplementedException("WindowsWallpaperService реализуется в M4.");
    }
}
