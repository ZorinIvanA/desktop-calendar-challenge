using DesktopCalendar.Core.Settings;
using SkiaSharp;

namespace DesktopCalendar.Core.Calendar;

/// <summary>
/// Рендер календаря на Skia поверх фонового изображения.
///
/// Контракт: принимает background, УЖЕ приведённый к размеру канваса монитора
/// (это работа M4 WallpaperApplier). Рисует календарь в координатах этого канваса.
///
/// Размер шрифта интерпретируется как pt и переводится в px по renderDpi.
/// renderDpi передаётся снаружи, чтобы рендер оставался OS-agnostic.
/// </summary>
public sealed class CalendarRenderer : IDisposable
{
    private readonly SKFontManager _fontManager;
    private readonly CalendarModel _model;

    /// <summary>Краткие названия дней недели (Mo..Su).</summary>
    private static readonly string[] Weekdays = CalendarModel.WeekdayLabels.ToArray();

    public CalendarRenderer(int year, int month, DateOnly today)
    {
        _fontManager = SKFontManager.Default;
        _model = CalendarModel.Build(year, month, today);
    }

    /// <summary>
    /// Нарисовать календарь поверх background. Канвас background не модифицируется;
    /// создаётся копия, на которой рисуется календарь.
    /// </summary>
    /// <param name="background">Фон размером monitorW×monitorH.</param>
    /// <param name="settings">Настройки календаря.</param>
    /// <param name="renderDpi">DPI для перевода pt→px (напр. 96).</param>
    public SKBitmap Render(SKBitmap background, AppSettings settings, double renderDpi)
    {
        // Копируем фон, чтобы не портить исходный bitmap.
        var canvas = new SKBitmap(background.Info);
        using (var sk = new SKCanvas(canvas))
        {
            sk.DrawBitmap(background, 0, 0);
        }
        return RenderInPlace(canvas, settings, renderDpi);
    }

    /// <summary>Перегрузка для фона в виде сплошного цвета (для smoke/превью).</summary>
    public SKBitmap Render(SKColor backgroundColor, double monitorW, double monitorH, AppSettings settings, double renderDpi)
    {
        var info = new SKImageInfo((int)monitorW, (int)monitorH, SKColorType.Bgra8888, SKAlphaType.Premul);
        var canvas = new SKBitmap(info);
        using (var sk = new SKCanvas(canvas))
        {
            sk.Clear(backgroundColor);
        }
        return RenderInPlace(canvas, settings, renderDpi);
    }

    /// <summary>Фактическая отрисовка на canvas-копии.</summary>
    private SKBitmap RenderInPlace(SKBitmap target, AppSettings settings, double renderDpi)
    {
        double fontPx = settings.FontSize * renderDpi / 72.0;
        var font = CreateFont(settings.FontFamily, (float)fontPx);
        using (font)
        using (var paint = new SKPaint { IsAntialias = true })
        using (var sk = new SKCanvas(target))
        {
            var measured = MeasureLayout(font, paint);
            var rect = LayoutCalculator.Place(
                settings.Anchor, settings.MarginPx, target.Width, target.Height, measured);

            DrawCalendar(sk, font, paint, settings, rect);
        }
        return target;
    }

    private SKFont CreateFont(string fontFamily, float sizePx)
    {
        var style = new SKFontStyle(SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);
        var tf = _fontManager.MatchFamily(fontFamily, style) ?? SKTypeface.Default;
        return new SKFont(tf, sizePx);
    }

    /// <summary>
    /// Измерить общий размер блока календаря: 7 колонок дней + строка дней недели + строка месяца.
    /// Ширина = 7 × cellWidth + поля; высота = monthLine + weekdayLine + 6 × cellHeight + поля.
    /// </summary>
    private MeasureSize MeasureLayout(SKFont font, SKPaint paint)
    {
        // Худший случай ширины ячейки — двухзначный день "31". Высота строки — по метрикам шрифта.
        font.MeasureText("31", out var dayBounds, paint);
        double cellW = Math.Max(dayBounds.Width, 0) * 1.6;   // запас под выравнивание/поля ячейки
        double cellH = (font.Metrics.Descent - font.Metrics.Ascent + font.Metrics.Leading) * 1.4;

        // Ширина строки месяца — по полному названию (напр. "September 2026" — самый длинный месяц).
        font.MeasureText("September 2026", out var monthBounds, paint);
        double monthW = Math.Max(monthBounds.Width, 0);

        // Высота строк месяца и дней недели — как одна строка шрифта + поля.
        double labelH = (font.Metrics.Descent - font.Metrics.Ascent + font.Metrics.Leading) * 1.5;

        const double outerPad = 12.0; // внутренний padding всего блока

        double width = Math.Max(cellW * 7, monthW) + outerPad * 2;
        double height = labelH + labelH + cellH * 6 + outerPad * 2; // месяц + дни недели + 6 строк дней

        return new MeasureSize(width, height);
    }

    private void DrawCalendar(SKCanvas sk, SKFont font, SKPaint paint, AppSettings settings, CalendarRect rect)
    {
        const double outerPad = 12.0;
        var model = _model;

        // Размеры строк.
        double fontLineH = font.Metrics.Descent - font.Metrics.Ascent + font.Metrics.Leading;
        double cellW = (rect.Width - outerPad * 2) / 7.0;
        double cellH = (rect.Height - outerPad * 2 - fontLineH * 1.5 * 2) / 6.0; // 6 строк дней
        double labelH = fontLineH * 1.5;

        // Координаты зон в зависимости от LabelsPosition.
        double blockX = rect.X + outerPad;
        double blockRight = rect.X + rect.Width - outerPad;
        double blockY = rect.Y + outerPad;

        double monthY, weekdayY, gridY;
        if (settings.LabelsPosition == LabelsPosition.Top)
        {
            monthY = blockY + fontLineH;          // baseline месяца
            weekdayY = monthY + labelH;            // baseline дней недели
            gridY = blockY + labelH * 2;           // верх сетки дней
        }
        else
        {
            gridY = blockY;                         // сетка сверху
            weekdayY = blockY + cellH * 6 + fontLineH;     // baseline дней недели под сеткой
            monthY = weekdayY + labelH;             // месяц под днями недели
        }

        // --- Строка месяца (по центру блока по горизонтали) ---
        paint.Color = settings.ColorMonth.ToSkColor();
        font.MeasureText(model.MonthTitle, out var mt, paint);
        float monthX = (float)((blockX + blockRight) / 2 - mt.Width / 2 - mt.Left);
        sk.DrawText(model.MonthTitle, monthX, (float)monthY, SKTextAlign.Left, font, paint);

        // --- Строка дней недели ---
        paint.Color = settings.ColorWeekday.ToSkColor();
        for (int col = 0; col < 7; col++)
        {
            var label = Weekdays[col];
            font.MeasureText(label, out var wb, paint);
            float cx = (float)(blockX + col * cellW + cellW / 2 - wb.Width / 2 - wb.Left);
            sk.DrawText(label, cx, (float)weekdayY, SKTextAlign.Left, font, paint);
        }

        // --- Сетка дней ---
        for (int row = 0; row < 6; row++)
        {
            for (int col = 0; col < 7; col++)
            {
                var cell = model.Grid[row][col];

                // Соседние дни в режиме Hide не рисуются вовсе.
                if (!cell.IsCurrentMonth && settings.OtherMonthMode == OtherMonthMode.Hide)
                {
                    continue;
                }

                // Базовый цвет по приоритету today > weekend > day — для ВСЕХ ячеек,
                // включая соседние месяцы (выходные подсвечиваются по всей сетке).
                var baseColor = ResolveCellColor(cell, settings);

                // Для соседних месяцев — применяем OtherMonth-режим к цвету.
                if (!cell.IsCurrentMonth && settings.OtherMonthMode == OtherMonthMode.CustomOpacity)
                {
                    baseColor = baseColor.WithOpacityPercent(settings.OtherMonthOpacity);
                }

                paint.Color = baseColor;

                var text = cell.Day.ToString();
                font.MeasureText(text, out var db, paint);
                float cx = (float)(blockX + col * cellW + cellW / 2 - db.Width / 2 - db.Left);
                float cy = (float)(gridY + row * cellH + cellH / 2 + fontLineH / 2 - font.Metrics.Descent / 2);
                sk.DrawText(text, cx, cy, SKTextAlign.Left, font, paint);
            }
        }
    }

    /// <summary>Приоритет цвета ячейки: today &gt; weekend &gt; day.</summary>
    private static SKColor ResolveCellColor(CalendarCell cell, AppSettings settings)
    {
        if (cell.IsToday && settings.ColorToday.HasValue)
        {
            return settings.ColorToday.Value.ToSkColor();
        }
        if (cell.IsWeekend && settings.ColorWeekend.HasValue)
        {
            return settings.ColorWeekend.Value.ToSkColor();
        }
        return settings.ColorDay.ToSkColor();
    }

    public void Dispose()
    {
        _fontManager.Dispose();
    }
}
