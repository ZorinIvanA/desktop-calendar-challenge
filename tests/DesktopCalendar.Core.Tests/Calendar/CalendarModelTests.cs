using DesktopCalendar.Core.Calendar;

namespace DesktopCalendar.Core.Tests.Calendar;

/// <summary>
/// Тесты модели месяца: размер сетки 6×7,周一-first, соседние дни, флаги.
/// </summary>
public sealed class CalendarModelTests
{
    [Theory]
    [InlineData(2026, 7)]
    [InlineData(2026, 2)]
    [InlineData(2025, 12)]
    [InlineData(2024, 1)]  // високосный год рядом
    public void Build_Always6x7Grid(int year, int month)
    {
        var model = CalendarModel.Build(year, month, new DateOnly(2026, 7, 10));

        model.Grid.Should().HaveCount(6);
        foreach (var row in model.Grid)
        {
            row.Should().HaveCount(7);
        }
    }

    [Fact]
    public void July2026_FirstIsWednesday_TwoDaysLeadFromJune()
    {
        // 1 июля 2026 — среда. В Mo-first схеме ей соответствует столбец 2 (Mo,Tu lead).
        var model = CalendarModel.Build(2026, 7, new DateOnly(2026, 7, 10));

        model.Grid[0][0].Date.Should().Be(new DateOnly(2026, 6, 29)); // понедельник до 1-го
        model.Grid[0][1].Date.Should().Be(new DateOnly(2026, 6, 30)); // вторник
        model.Grid[0][2].Date.Should().Be(new DateOnly(2026, 7, 1));  // среда = 1-е
        model.Grid[0][2].IsCurrentMonth.Should().BeTrue();
        model.Grid[0][0].IsCurrentMonth.Should().BeFalse();
    }

    [Fact]
    public void November2026_FirstIsSunday_NoLead()
    {
        // 1 ноября 2026 — воскресенье. В Mo-first это последний столбец первой строки.
        var model = CalendarModel.Build(2026, 11, new DateOnly(2026, 11, 15));

        // Все 6 дней слева от 1-го — конец октября.
        for (int col = 0; col < 6; col++)
        {
            model.Grid[0][col].IsCurrentMonth.Should().BeFalse($"col {col} — октябрь");
            model.Grid[0][col].Date.Month.Should().Be(10);
        }
        model.Grid[0][6].Date.Should().Be(new DateOnly(2026, 11, 1));
        model.Grid[0][6].IsCurrentMonth.Should().BeTrue();
    }

    [Fact]
    public void WeekendFlags_SaturdayAndSundayOnly()
    {
        var model = CalendarModel.Build(2026, 7, new DateOnly(2026, 7, 10));

        for (int row = 0; row < 6; row++)
        {
            for (int col = 0; col < 7; col++)
            {
                var expectedWeekend = col == 5 || col == 6;
                model.Grid[row][col].IsWeekend.Should().Be(expectedWeekend,
                    $"col {col} ({model.Grid[row][col].Date})");
            }
        }
    }

    [Fact]
    public void TodayFlag_OnlyOnToday()
    {
        var today = new DateOnly(2026, 7, 10);
        var model = CalendarModel.Build(2026, 7, today);

        var todayCells = model.Grid.SelectMany(r => r).Where(c => c.IsToday).ToList();
        todayCells.Should().ContainSingle();
        todayCells[0].Date.Should().Be(today);
        todayCells[0].IsCurrentMonth.Should().BeTrue();
    }

    [Fact]
    public void Today_InOtherMonth_StillFlagged()
    {
        // today = 30 июня, смотрим июль → 30 июня должна быть помечена IsToday, но не IsCurrentMonth.
        var model = CalendarModel.Build(2026, 7, new DateOnly(2026, 6, 30));

        var today = model.Grid.SelectMany(r => r).Single(c => c.IsToday);
        today.Date.Should().Be(new DateOnly(2026, 6, 30));
        today.IsCurrentMonth.Should().BeFalse();
    }

    [Fact]
    public void GridCovers42ConsecutiveDays()
    {
        var model = CalendarModel.Build(2026, 7, new DateOnly(2026, 7, 10));

        var all = model.Grid.SelectMany(r => r).Select(c => c.Date).ToList();
        all.Should().HaveCount(42);
        for (int i = 1; i < all.Count; i++)
        {
            (all[i].DayNumber - all[i - 1].DayNumber).Should().Be(1, "дни идут подряд");
        }
    }

    [Fact]
    public void MonthTitle_IsEnUsLongMonthPlusYear()
    {
        var model = CalendarModel.Build(2026, 7, new DateOnly(2026, 7, 10));
        model.MonthTitle.Should().Be("July 2026");
    }

    [Fact]
    public void WeekdayLabels_MondayFirst_TwoLetterEnUs()
    {
        CalendarModel.WeekdayLabels.Should().Equal("Mo", "Tu", "We", "Th", "Fr", "Sa", "Su");
    }
}
