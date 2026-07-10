namespace DesktopCalendar.Core.Calendar;

/// <summary>
/// Одна ячейка сетки месяца (включая дни соседних месяцев).
/// </summary>
/// <param name="Date">Дата ячейки.</param>
/// <param name="IsCurrentMonth">Принадлежит ли отображаемому месяцу.</param>
/// <param name="IsToday">Является ли сегодняшним днём.</param>
/// <param name="IsWeekend">Суббота или воскресенье.</param>
public readonly record struct CalendarCell(DateOnly Date, bool IsCurrentMonth, bool IsToday, bool IsWeekend)
{
    /// <summary>Номер дня месяца (1..31).</summary>
    public int Day => Date.Day;
}
