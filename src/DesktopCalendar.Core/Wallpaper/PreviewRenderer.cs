using DesktopCalendar.Core.Calendar;
using DesktopCalendar.Core.Contracts;
using DesktopCalendar.Core.Settings;
using SkiaSharp;

namespace DesktopCalendar.Core.Wallpaper;

/// <summary>
/// Рендер превью календаря в натуральную величину на фоне фрагмента реальных обоев
/// выбранного монитора. OS-agnostic: использует BackgroundFitter + CalendarRenderer +
/// платформенный IWallpaperService для чтения текущего фона.
///
/// Возвращает фрагмент канваса монитора вокруг прямоугольника календаря с запасом.
/// Календарь внутри рисуется в реальном масштабе; дальнейшее масштабирование до UI
/// делает уже Avalonia (Image.Stretch).
/// </summary>
public sealed class PreviewRenderer
{
    private readonly IWallpaperService _wallpaper;
    private readonly IMonitorService _monitors;
    private readonly IPlatformPaths _paths;

    public PreviewRenderer(IWallpaperService wallpaper, IMonitorService monitors, IPlatformPaths paths)
    {
        _wallpaper = wallpaper;
        _monitors = monitors;
        _paths = paths;
    }

    /// <summary>
    /// Отрендерить превью. Возвращает null, если монитор не выбран.
    /// </summary>
    public SKBitmap? RenderPreview(AppSettings settings, DateOnly today, double renderDpi = 96.0)
    {
        var monitorId = settings.TargetMonitorId;
        if (string.IsNullOrEmpty(monitorId)) return null;

        var monitor = _monitors.GetById(monitorId);
        if (monitor is null) return null;

        // 1. Фон: оригинал (из OriginalsDir) или текущие обои системы.
        using var original = LoadBackground(monitorId, settings);
        WallpaperFit fit = settings.OriginalFit ?? _wallpaper.ParseCurrentFit(monitorId);
        using var canvas = BackgroundFitter.FitToMonitor(
            original, monitor.ResolutionWidth, monitor.ResolutionHeight, fit);

        // 2. Календарь поверх (натуральная величина).
        using var renderer = new CalendarRenderer(today.Year, today.Month, today);
        using var withCalendar = renderer.Render(canvas, settings, renderDpi);

        // 3. Вырезать фрагмент вокруг календаря.
        return CropAroundCalendar(withCalendar, settings, monitor.ResolutionWidth, monitor.ResolutionHeight, renderer);
    }

    private SKBitmap LoadBackground(string monitorId, AppSettings settings)
    {
        // Приоритет: сохранённый оригинал (если уже применяли), иначе текущие обои системы.
        var saved = settings.LastOriginalPath;
        if (!string.IsNullOrEmpty(saved) && File.Exists(saved) && SKBitmap.Decode(saved) is { } bmp1)
        {
            return bmp1;
        }

        var snapshot = _wallpaper.GetCurrent(monitorId);
        if (!string.IsNullOrEmpty(snapshot.CurrentUri) && File.Exists(snapshot.CurrentUri))
        {
            return SKBitmap.Decode(snapshot.CurrentUri) ?? CreateFallback();
        }
        return CreateFallback();
    }

    private static SKBitmap CreateFallback()
    {
        var bmp = new SKBitmap(new SKImageInfo(1920, 1080, SKColorType.Bgra8888, SKAlphaType.Premul));
        using var sk = new SKCanvas(bmp);
        var shader = SKShader.CreateLinearGradient(
            new SKPoint(0, 0), new SKPoint(1920, 1080),
            new[] { new SKColor(0x2b, 0x58, 0x76), new SKColor(0x4e, 0x43, 0x72), new SKColor(0x6a, 0x30, 0x93) },
            SKShaderTileMode.Clamp);
        using var p = new SKPaint { Shader = shader };
        sk.DrawRect(0, 0, 1920, 1080, p);
        return bmp;
    }

    /// <summary>
    /// Вырезать фрагмент канваса монитора вокруг прямоугольника календаря.
    /// Запас = 40% от размера календаря с каждой стороны, но в пределах канваса.
    /// </summary>
    private static SKBitmap CropAroundCalendar(SKBitmap full, AppSettings settings, int monitorW, int monitorH, CalendarRenderer renderer)
    {
        // Измерить размер календаря тем же шрифтом, что использует рендерер.
        double fontPx = settings.FontSize * 96.0 / 72.0;
        using var font = CreateMeasureFont(settings.FontFamily, (float)fontPx);
        using var paint = new SKPaint();
        var measured = MeasureLayout(font, paint);
        var rect = LayoutCalculator.Place(settings.Anchor, settings.MarginPx, monitorW, monitorH, measured);

        // Запас вокруг: 40% от размеров календаря, но не менее 60px.
        double padX = Math.Max(measured.Width * 0.4, 60);
        double padY = Math.Max(measured.Height * 0.4, 60);

        int cropX = (int)Math.Clamp(rect.X - padX, 0, Math.Max(0, monitorW - 1));
        int cropY = (int)Math.Clamp(rect.Y - padY, 0, Math.Max(0, monitorH - 1));
        int cropX2 = (int)Math.Clamp(rect.X + rect.Width + padX, 1, monitorW);
        int cropY2 = (int)Math.Clamp(rect.Y + rect.Height + padY, 1, monitorH);

        int w = cropX2 - cropX;
        int h = cropY2 - cropY;
        if (w <= 0 || h <= 0)
        {
            // Если что-то пошло не так — вернём весь канвас.
            return full.Copy();
        }

        var subset = new SKBitmap(new SKImageInfo(w, h, SKColorType.Bgra8888, SKAlphaType.Premul));
        using (var sk = new SKCanvas(subset))
        {
            var srcRect = new SKRect(cropX, cropY, cropX2, cropY2);
            var dstRect = new SKRect(0, 0, w, h);
            sk.DrawBitmap(full, srcRect, dstRect);
        }
        return subset;
    }

    private static SKFont CreateMeasureFont(string fontFamily, float sizePx)
    {
        var style = new SKFontStyle(SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);
        var tf = SKFontManager.Default.MatchFamily(fontFamily, style) ?? SKTypeface.Default;
        return new SKFont(tf, sizePx);
    }

    /// <summary>Копия логики измерения из CalendarRenderer.MeasureLayout.</summary>
    private static MeasureSize MeasureLayout(SKFont font, SKPaint paint)
    {
        font.MeasureText("31", out var dayBounds, paint);
        double cellW = Math.Max(dayBounds.Width, 0) * 1.6;
        double cellH = (font.Metrics.Descent - font.Metrics.Ascent + font.Metrics.Leading) * 1.4;
        font.MeasureText("September 2026", out var monthBounds, paint);
        double monthW = Math.Max(monthBounds.Width, 0);
        double labelH = (font.Metrics.Descent - font.Metrics.Ascent + font.Metrics.Leading) * 1.5;
        const double outerPad = 12.0;
        double width = Math.Max(cellW * 7, monthW) + outerPad * 2;
        double height = labelH + labelH + cellH * 6 + outerPad * 2;
        return new MeasureSize(width, height);
    }
}
