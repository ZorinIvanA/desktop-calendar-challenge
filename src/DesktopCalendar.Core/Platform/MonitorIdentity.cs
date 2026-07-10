namespace DesktopCalendar.Core.Platform;

/// <summary>
/// Идентификационная информация об одном мониторе из платформенного источника
/// (Windows IDesktopWallpaper / Linux Mutter D-Bus). Геометрия сюда НЕ входит —
/// она приходит из IGeometryProvider и сшивается в MonitorService по Bounds.
/// </summary>
/// <param name="Id">Стабильный идентификатор: device path (Win) / vendor:product:serial или connector (Linux).</param>
/// <param name="Connector">Техническое имя: "DP-1", "HDMI-A-1" — для отображения в UI.</param>
/// <param name="DisplayName">Человекочитаемое имя монитора из EDID (напр. "LG Ultra HD"), если доступно.</param>
/// <param name="LogicalIndex">1-based порядковый номер, как видит пользователь (1, 2, …).</param>
/// <param name="Bounds">Геометрия из платформенного источника, для сопоставления с Avalonia-экраном.</param>
public sealed record MonitorIdentity(
    string Id,
    string? Connector,
    string? DisplayName,
    int LogicalIndex,
    ScreenRect Bounds);
