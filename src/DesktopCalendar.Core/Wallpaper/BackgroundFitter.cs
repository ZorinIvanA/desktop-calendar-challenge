using DesktopCalendar.Core.Contracts;
using SkiaSharp;

namespace DesktopCalendar.Core.Wallpaper;

/// <summary>
/// Приведение фонового изображения к размеру монитора по режиму растяжения ОС.
/// Результат ВСЕГДА имеет размер monitorW × monitorH (канвас монитора) — то, что видит пользователь
/// как фон. На этот канвас далее накладывается календарь.
///
/// Режимы соответствуют поведению GNOME picture-options / Windows DWPOS:
/// - Fill (zoom): масштабировать с сохранением пропорций до заполнения, лишнее обрезать.
/// - Fit (scaled): вписать с полями (дефектное поле заливается чёрным).
/// - Stretch: растянуть без сохранения пропорций.
/// - Center: 1:1 по центру, всё что не влезло — обрезано; меньше монитора — поле чёрное.
/// - Tile: замостить 1:1 повторами.
/// - Span: через все мониторы одним изображением — для одного монитора ≈ Fill.
/// - None: сплошной чёрный (как заглушка, используется когда оригинал = 'none').
/// </summary>
public static class BackgroundFitter
{
    public static SKBitmap FitToMonitor(SKBitmap original, int monitorW, int monitorH, WallpaperFit fit)
    {
        var info = new SKImageInfo(monitorW, monitorH, SKColorType.Bgra8888, SKAlphaType.Premul);
        var canvas = new SKBitmap(info);
        using (var sk = new SKCanvas(canvas))
        {
            sk.Clear(SKColors.Black);

            switch (fit)
            {
                case WallpaperFit.None:
                    break; // чёрный канвас
                case WallpaperFit.Stretch:
                    sk.DrawBitmap(original, new SKRect(0, 0, monitorW, monitorH));
                    break;
                case WallpaperFit.Center:
                    DrawCentered(sk, original, monitorW, monitorH);
                    break;
                case WallpaperFit.Tile:
                    DrawTiled(sk, original, monitorW, monitorH);
                    break;
                case WallpaperFit.Fill:
                case WallpaperFit.Span:
                    DrawCover(sk, original, monitorW, monitorH);
                    break;
                case WallpaperFit.Fit:
                default:
                    DrawContain(sk, original, monitorW, monitorH);
                    break;
            }
        }
        return canvas;
    }

    /// <summary>Масштаб "cover" — заполнить с обрезкой.</summary>
    private static void DrawCover(SKCanvas sk, SKBitmap src, int w, int h)
    {
        var scale = Math.Max((double)w / src.Width, (double)h / src.Height);
        var newW = src.Width * scale;
        var newH = src.Height * scale;
        var dx = (w - newW) / 2;
        var dy = (h - newH) / 2;
        var dst = new SKRect((float)dx, (float)dy, (float)(dx + newW), (float)(dy + newH));
        sk.DrawBitmap(src, dst);
    }

    /// <summary>Масштаб "contain" — вписать с полями.</summary>
    private static void DrawContain(SKCanvas sk, SKBitmap src, int w, int h)
    {
        var scale = Math.Min((double)w / src.Width, (double)h / src.Height);
        var newW = src.Width * scale;
        var newH = src.Height * scale;
        var dx = (w - newW) / 2;
        var dy = (h - newH) / 2;
        var dst = new SKRect((float)dx, (float)dy, (float)(dx + newW), (float)(dy + newH));
        sk.DrawBitmap(src, dst);
    }

    /// <summary>1:1 по центру, без масштабирования; обрезает всё, что выходит за экран.</summary>
    private static void DrawCentered(SKCanvas sk, SKBitmap src, int w, int h)
    {
        var dx = (w - src.Width) / 2;
        var dy = (h - src.Height) / 2;

        if (dx >= 0 && dy >= 0)
        {
            // Изображение меньше экрана — рисуем целиком по центру.
            sk.DrawBitmap(src, (float)dx, (float)dy);
            return;
        }
        // Изображение больше экрана — берём центральную часть исходника размером w×h.
        int srcX = src.Width > w ? (src.Width - w) / 2 : 0;
        int srcY = src.Height > h ? (src.Height - h) / 2 : 0;
        int takeW = Math.Min(w, src.Width);
        int takeH = Math.Min(h, src.Height);
        var srcRect = new SKRectI(srcX, srcY, srcX + takeW, srcY + takeH);
        var dstRect = new SKRect(
            src.Width > w ? 0 : (w - src.Width) / 2f,
            src.Height > h ? 0 : (h - src.Height) / 2f,
            src.Width > w ? w : (w - src.Width) / 2f + src.Width,
            src.Height > h ? h : (h - src.Height) / 2f + src.Height);
        sk.DrawBitmap(src, srcRect, dstRect);
    }

    /// <summary>Замостить 1:1 повторами от левого-верхнего угла.</summary>
    private static void DrawTiled(SKCanvas sk, SKBitmap src, int w, int h)
    {
        var shader = SKShader.CreateBitmap(src, SKShaderTileMode.Repeat, SKShaderTileMode.Repeat);
        using var paint = new SKPaint { Shader = shader };
        sk.DrawRect(0, 0, w, h, paint);
    }
}
