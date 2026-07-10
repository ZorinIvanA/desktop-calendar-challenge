using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using DesktopCalendar.Core.Contracts;
using DesktopCalendar.Core.Wallpaper;

namespace DesktopCalendar.Platform.Windows;

/// <summary>
/// Windows-реализация IWallpaperService через COM IDesktopWallpaper.
/// Per-monitor: SetWallpaper(monitorId, path) применяет к конкретному монитору.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsWallpaperService : IWallpaperService
{
    private static IDesktopWallpaper CreateCom()
    {
        var type = Type.GetTypeFromCLSID(DesktopWallpaperClsid.Value)
                   ?? throw new InvalidOperationException("DesktopWallpaper COM CLSID not found.");
        return (IDesktopWallpaper)Activator.CreateInstance(type)!;
    }

    public WallpaperSnapshot GetCurrent(string monitorId)
    {
        if (!OperatingSystem.IsWindows())
        {
            return new WallpaperSnapshot(string.Empty, null, false);
        }

        var wallpaper = CreateCom();
        try
        {
            string current = wallpaper.GetWallpaper(monitorId) ?? string.Empty;
            bool hasCalendar = GeneratedFileMarker.IsGenerated(current);
            return new WallpaperSnapshot(current, hasCalendar ? current : null, hasCalendar);
        }
        finally
        {
            Marshal.ReleaseComObject(wallpaper);
        }
    }

    public void SetWallpaper(string monitorId, string imageFilePath)
    {
        var wallpaper = CreateCom();
        try { wallpaper.SetWallpaper(monitorId, imageFilePath); }
        finally { Marshal.ReleaseComObject(wallpaper); }
    }

    public string? GetOriginalPath(string monitorId) => null;

    public WallpaperFit ParseCurrentFit(string monitorId)
    {
        var wallpaper = CreateCom();
        try { return WallpaperFitMapping.FromWindowsPosition((int)wallpaper.GetPosition()); }
        finally { Marshal.ReleaseComObject(wallpaper); }
    }

    public void SetFit(string monitorId, WallpaperFit fit)
    {
        var wallpaper = CreateCom();
        try { wallpaper.SetPosition((DesktopWallpaperPosition)WallpaperFitMapping.ToWindowsPosition(fit)); }
        finally { Marshal.ReleaseComObject(wallpaper); }
    }
}
