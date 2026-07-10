using DesktopCalendar.Core.Contracts;

namespace DesktopCalendar.Core.Wallpaper;

/// <summary>
/// Маппинг унифицированного enum WallpaperFit ↔ платформенные представления:
/// Windows DESKTOP_WALLPAPER_POSITION (int) и GNOME picture-options (string).
/// Чистые функции, unit-тестируемые без обращения к ОС.
/// </summary>
public static class WallpaperFitMapping
{
    // Windows: DWPOS_CENTER=0, TILE=1, STRETCH=2, FIT=3, FILL=4, SPAN=5.
    public static int ToWindowsPosition(WallpaperFit fit) => fit switch
    {
        WallpaperFit.Center => 0,
        WallpaperFit.Tile => 1,
        WallpaperFit.Stretch => 2,
        WallpaperFit.Fit => 3,
        WallpaperFit.Fill => 4,
        WallpaperFit.Span => 5,
        _ => 0,
    };

    public static WallpaperFit FromWindowsPosition(int position) => position switch
    {
        0 => WallpaperFit.Center,
        1 => WallpaperFit.Tile,
        2 => WallpaperFit.Stretch,
        3 => WallpaperFit.Fit,
        4 => WallpaperFit.Fill,
        5 => WallpaperFit.Span,
        _ => WallpaperFit.Center,
    };

    // GNOME picture-options: none | wallpaper | centered | scaled | stretched | zoom | spanned.
    public static string ToGnomeOptions(WallpaperFit fit) => fit switch
    {
        WallpaperFit.None => "none",
        WallpaperFit.Tile => "wallpaper",
        WallpaperFit.Center => "centered",
        WallpaperFit.Fit => "scaled",
        WallpaperFit.Stretch => "stretched",
        WallpaperFit.Fill => "zoom",
        WallpaperFit.Span => "spanned",
        _ => "centered",
    };

    public static WallpaperFit FromGnomeOptions(string options) => (options ?? "").Trim('\'', '"').Trim() switch
    {
        "none" => WallpaperFit.None,
        "wallpaper" => WallpaperFit.Tile,
        "centered" => WallpaperFit.Center,
        "scaled" => WallpaperFit.Fit,
        "stretched" => WallpaperFit.Stretch,
        "zoom" => WallpaperFit.Fill,
        "spanned" => WallpaperFit.Span,
        _ => WallpaperFit.Center,
    };
}
