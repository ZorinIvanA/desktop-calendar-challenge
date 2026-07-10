using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using DesktopCalendar.Core.Contracts;
using DesktopCalendar.Core.Wallpaper;

namespace DesktopCalendar.Platform.Windows;

/// <summary>
/// Windows-реализация IWallpaperService через COM IDesktopWallpaper.
/// Per-monitor: SetWallpaper(monitorId, path) применяет к конкретному монитору.
/// HasCalendar-детект: текущий путь == пути нашего сгенерированного файла.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsWallpaperService : IWallpaperService
{
    private static IDesktopWallpaper CreateCom()
    {
        var type = Type.GetTypeFromCLSID(IDesktopWallpaper.Clsid)
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
            // Идентификация "наш ли это файл" — по соглашению об имени в GeneratedDir.
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
        try
        {
            wallpaper.SetWallpaper(monitorId, imageFilePath);
        }
        finally
        {
            Marshal.ReleaseComObject(wallpaper);
        }
    }

    public string? GetOriginalPath(string monitorId)
    {
        // Оригинал хранится WallpaperApplier'ом в OriginalsDir; сервис не знает путей приложения.
        // В M2 контракт предполагал, что сервис отдаёт оригинал, но фактически это ответственность
        // оркестратора (он знает IPlatformPaths). Здесь возвращаем null — апликатор использует свой кэш.
        return null;
    }

    public WallpaperFit ParseCurrentFit(string monitorId)
    {
        var wallpaper = CreateCom();
        try
        {
            var pos = wallpaper.GetPosition();
            return WallpaperFitMapping.FromWindowsPosition((int)pos);
        }
        finally
        {
            Marshal.ReleaseComObject(wallpaper);
        }
    }

    public void SetFit(string monitorId, WallpaperFit fit)
    {
        var wallpaper = CreateCom();
        try
        {
            wallpaper.SetPosition((DesktopWallpaperPosition)WallpaperFitMapping.ToWindowsPosition(fit));
        }
        finally
        {
            Marshal.ReleaseComObject(wallpaper);
        }
    }
}
