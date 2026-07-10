namespace DesktopCalendar.Core.Contracts;

/// <summary>
/// Режим растяжения обоев, как его понимает ОС. Унифицирован между Windows и GNOME.
/// </summary>
public enum WallpaperFit
{
    /// <summary>Заполнить с обрезкой (Windows: Fill, GNOME: zoom).</summary>
    Fill,
    /// <summary>Вписать с полями (Windows: Fit, GNOME: scaled).</summary>
    Fit,
    /// <summary>Растянуть (Windows: Stretch, GNOME: stretched).</summary>
    Stretch,
    /// <summary>По центру 1:1 (Windows: Center, GNOME: centered).</summary>
    Center,
    /// <summary>Замостить (Windows: Tile, GNOME: wallpaper).</summary>
    Tile,
    /// <summary>Через все мониторы одним изображением (Windows: Span, GNOME: spanned).</summary>
    Span,
}
