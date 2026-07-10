using DesktopCalendar.Core.Contracts;
using DesktopCalendar.Core.Wallpaper;

namespace DesktopCalendar.Platform.Linux;

/// <summary>
/// GNOME-реализация IWallpaperService через gsettings org.gnome.desktop.background.
///
/// Ограничение GNOME (TZ §2.2): единый URI обоев на всю сессию. monitorId используется только
/// для маршрутизации в OriginalsDir; фактически меняется общий фон.
/// HasCalendar-детект: текущий picture-uri указывает на наш сгенерированный файл.
/// Тёмная тема: пишем/читаем оба picture-uri и picture-uri-dark.
/// </summary>
public sealed class LinuxWallpaperService : IWallpaperService
{
    private readonly GnomeGsettings _gsettings;

    public LinuxWallpaperService(GnomeGsettings gsettings)
    {
        _gsettings = gsettings;
    }

    public WallpaperSnapshot GetCurrent(string monitorId)
    {
        // Активный URI определяется color-scheme, но на практике для детекта "наш ли файл"
        // достаточно проверить оба (мы пишем оба).
        var raw = _gsettings.GetPictureUri();
        var path = raw is null ? null : GnomeUri.PathFromPictureUri(raw);

        bool hasCalendar = GeneratedFileMarker.IsGenerated(path);
        return new WallpaperSnapshot(
            CurrentUri: path ?? string.Empty,
            LastGeneratedPath: hasCalendar ? path : null,
            HasCalendar: hasCalendar);
    }

    public void SetWallpaper(string monitorId, string imageFilePath)
    {
        var gvariant = GnomeUri.ToGvariantUri(imageFilePath);
        _gsettings.SetPictureUri(gvariant);
    }

    public string? GetOriginalPath(string monitorId)
    {
        // Оригинал хранит WallpaperApplier в OriginalsDir; сервис не знает путей приложения.
        return null;
    }

    public WallpaperFit ParseCurrentFit(string monitorId)
    {
        var options = _gsettings.GetPictureOptions();
        return options is null ? WallpaperFit.Center : WallpaperFitMapping.FromGnomeOptions(options);
    }

    public void SetFit(string monitorId, WallpaperFit fit)
    {
        var options = WallpaperFitMapping.ToGnomeOptions(fit);
        _gsettings.SetPictureOptions("'" + options + "'");
    }
}
