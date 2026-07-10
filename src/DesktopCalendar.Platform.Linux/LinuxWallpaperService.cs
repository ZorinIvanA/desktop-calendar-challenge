using DesktopCalendar.Core.Contracts;

namespace DesktopCalendar.Platform.Linux;

/// <summary>
/// Linux-реализация IWallpaperService. В M1 — заглушка.
/// В M4: GNOME через gsettings org.gnome.desktop.background; прочие DE — PlatformNotSupportedException.
/// </summary>
public sealed class LinuxWallpaperService : IWallpaperService
{
    public WallpaperSnapshot GetCurrent(string monitorId)
    {
        // TODO M4.
        throw new NotImplementedException("LinuxWallpaperService реализуется в M4.");
    }

    public void SetWallpaper(string monitorId, string imageFilePath)
    {
        // TODO M4: gsettings set org.gnome.desktop.background picture-uri.
        throw new NotImplementedException("LinuxWallpaperService реализуется в M4.");
    }

    public string? GetOriginalPath(string monitorId)
    {
        // TODO M4.
        throw new NotImplementedException("LinuxWallpaperService реализуется в M4.");
    }

    public WallpaperFit ParseCurrentFit(string monitorId)
    {
        // TODO M4: gsettings get picture-options → маппинг в WallpaperFit.
        throw new NotImplementedException("LinuxWallpaperService реализуется в M4.");
    }
}
