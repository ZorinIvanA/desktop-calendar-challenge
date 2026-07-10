namespace DesktopCalendar.Core.Platform;

/// <summary>
/// Геометрия одного экрана из источника, не зависящего от ОС (Avalonia Screens).
/// Не содержит стабильного идентификатора монитора — только геометрию и отображаемое имя.
/// </summary>
public sealed record ScreenGeometry(
    ScreenRect Bounds,
    ScreenRect WorkingArea,
    double Scaling,
    bool IsPrimary,
    string? DisplayName);
