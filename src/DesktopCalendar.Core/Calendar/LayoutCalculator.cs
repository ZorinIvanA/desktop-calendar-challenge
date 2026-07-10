using DesktopCalendar.Core.Settings;

namespace DesktopCalendar.Core.Calendar;

/// <summary>Измеренный размер прямоугольника календаря (px).</summary>
public readonly record struct MeasureSize(double Width, double Height);

/// <summary>Размещённый прямоугольник календаря в координатах канваса (px).</summary>
public readonly record struct CalendarRect(double X, double Y, double Width, double Height);

/// <summary>
/// Чистая геометрия календаря, без Skia. Формулы anchor — из SPECIFICATION §5.2.
/// Разделяется на Measure (размер блока по измерениям ячеек) и Place (позиция по anchor).
/// </summary>
public static class LayoutCalculator
{
    /// <summary>
    /// Рассчитать прямоугольник календаря в координатах канваса размером monitorW×monitorH.
    /// margin — отступ от края экрана; measured — размер блока календаря.
    /// </summary>
    public static CalendarRect Place(Anchor anchor, int margin, double monitorW, double monitorH, MeasureSize measured)
    {
        double m = margin;
        double w = measured.Width;
        double h = measured.Height;

        double x = anchor switch
        {
            Anchor.TopLeft or Anchor.CenterLeft or Anchor.BottomLeft => m,
            Anchor.TopCenter or Anchor.BottomCenter => (monitorW - w) / 2,
            Anchor.TopRight or Anchor.CenterRight or Anchor.BottomRight => monitorW - w - m,
            _ => m,
        };

        double y = anchor switch
        {
            Anchor.TopLeft or Anchor.TopCenter or Anchor.TopRight => m,
            Anchor.CenterLeft or Anchor.CenterRight => (monitorH - h) / 2,
            Anchor.BottomLeft or Anchor.BottomCenter or Anchor.BottomRight => monitorH - h - m,
            _ => m,
        };

        return new CalendarRect(x, y, w, h);
    }
}
