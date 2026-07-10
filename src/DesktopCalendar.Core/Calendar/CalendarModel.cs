using System.Globalization;

namespace DesktopCalendar.Core.Calendar;

/// <summary>
/// Модель месяца для отрисовки: сетка 6 строк × 7 столбцов, неделя начинается с понедельника.
/// Первые ячейки могут принадлежать концу предыдущего месяца, последние — началу следующего.
/// </summary>
public sealed class CalendarModel
{
    /// <summary>Краткие названия дней недели, понедельник-первый (en-US по ТЗ).</summary>
    public static readonly IReadOnlyList<string> WeekdayLabels =
        new[] { "Mo", "Tu", "We", "Th", "Fr", "Sa", "Su" };

    private static readonly CultureInfo TitleCulture = CultureInfo.GetCultureInfo("en-US");

    public int Year { get; }
    public int Month { get; }

    /// <summary>Сетка [row][col], row=0..5, col=0..6 (Mo..Su).</summary>
    public CalendarCell[][] Grid { get; }

    /// <summary>Название месяца + год, напр. "July 2026" (en-US).</summary>
    public string MonthTitle =>
        new DateTime(Year, Month, 1).ToString("MMMM yyyy", TitleCulture);

    private CalendarModel(int year, int month, CalendarCell[][] grid)
    {
        Year = year;
        Month = month;
        Grid = grid;
    }

    /// <summary>Построить сетку 6×7 для месяца year/month относительно today.</summary>
    public static CalendarModel Build(int year, int month, DateOnly today)
    {
        var firstOfMonth = new DateOnly(year, month, 1);
        // DayOfWeek: Sunday=0..Saturday=6. Переводим к схеме "понедельник=0".
        int mondayBased = ((int)firstOfMonth.DayOfWeek + 6) % 7;

        // Дата левого-верхнего угла сетки (может быть из предыдущего месяца).
        var gridStart = firstOfMonth.AddDays(-mondayBased);

        var grid = new CalendarCell[6][];
        for (int row = 0; row < 6; row++)
        {
            grid[row] = new CalendarCell[7];
            for (int col = 0; col < 7; col++)
            {
                var date = gridStart.AddDays(row * 7 + col);
                bool isWeekend = col == 5 || col == 6; // Sa, Su
                grid[row][col] = new CalendarCell(
                    Date: date,
                    IsCurrentMonth: date.Month == month && date.Year == year,
                    IsToday: date == today,
                    IsWeekend: isWeekend);
            }
        }
        return new CalendarModel(year, month, grid);
    }
}
