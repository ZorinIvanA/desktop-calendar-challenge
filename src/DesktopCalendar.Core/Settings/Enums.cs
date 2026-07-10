namespace DesktopCalendar.Core.Settings;

/// <summary>
/// Точка привязки календаря на экране монитора. 4 угла + середины 4 рёбер.
/// </summary>
public enum Anchor
{
    TopLeft,
    TopCenter,
    TopRight,
    CenterLeft,
    CenterRight,
    BottomLeft,
    BottomCenter,
    BottomRight,
}

/// <summary>
/// Предустановленный цвет из фиксированной палитры ТЗ (RGB-значения в SPECIFICATION §3).
/// White добавлен как дефолт — единственный гарантированно читаемый на светлых обоях.
/// </summary>
public enum PresetColor
{
    White,
    Red,
    Green,
    Blue,
}

/// <summary>
/// Как отображать дни соседних месяцев в таблице.
/// </summary>
public enum OtherMonthMode
{
    /// <summary>Показывать как обычные дни.</summary>
    Show,
    /// <summary>Не рисовать.</summary>
    Hide,
    /// <summary>Рисовать с альфой OtherMonthOpacity/100.</summary>
    CustomOpacity,
}

/// <summary>
/// Где рисовать строку месяца и строку дней недели относительно блока дней.
/// </summary>
public enum LabelsPosition
{
    Top,
    Bottom,
}
