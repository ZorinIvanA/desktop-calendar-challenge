using DesktopCalendar.Core.Contracts;
using DesktopCalendar.Core.Wallpaper;
using SkiaSharp;

namespace DesktopCalendar.Core.Tests.Wallpaper;

/// <summary>
/// Тесты BackgroundFitter: для любого fit результат имеет размер монитора (канвас).
/// Не проверяем конкретные пиксели (хрупко), только инвариант размера + что фон не чёрный
/// (т.е. изображение реально отрисовано, кроме None).
/// </summary>
public sealed class BackgroundFitterTests
{
    private const int MW = 1920;
    private const int MH = 1080;

    private static SKBitmap MakeSrc(int w, int h, SKColor color)
    {
        var bmp = new SKBitmap(new SKImageInfo(w, h, SKColorType.Bgra8888, SKAlphaType.Premul));
        using var sk = new SKCanvas(bmp);
        sk.Clear(color);
        return bmp;
    }

    [Theory]
    [InlineData(WallpaperFit.Fill)]
    [InlineData(WallpaperFit.Fit)]
    [InlineData(WallpaperFit.Stretch)]
    [InlineData(WallpaperFit.Center)]
    [InlineData(WallpaperFit.Tile)]
    [InlineData(WallpaperFit.Span)]
    public void Output_AlwaysMonitorSize_WideSource(WallpaperFit fit)
    {
        using var src = MakeSrc(1000, 500, SKColors.Red);
        using var result = BackgroundFitter.FitToMonitor(src, MW, MH, fit);

        result.Width.Should().Be(MW);
        result.Height.Should().Be(MH);
    }

    [Theory]
    [InlineData(WallpaperFit.Fill)]
    [InlineData(WallpaperFit.Stretch)]
    public void Output_DrawContent_NonBlackPixels(WallpaperFit fit)
    {
        // Источник красный, во весь экран должен быть красный (Fill/Stretch заливают полностью).
        using var src = MakeSrc(1000, 500, SKColors.Red);
        using var result = BackgroundFitter.FitToMonitor(src, 100, 100, fit);

        result.GetPixel(0, 0).Should().Be(SKColors.Red);
        result.GetPixel(99, 99).Should().Be(SKColors.Red);
    }

    [Fact]
    public void None_AllBlackCanvas()
    {
        using var src = MakeSrc(100, 100, SKColors.Red);
        using var result = BackgroundFitter.FitToMonitor(src, 50, 50, WallpaperFit.None);

        result.GetPixel(0, 0).Should().Be(SKColors.Black);
        result.GetPixel(25, 25).Should().Be(SKColors.Black);
    }

    [Fact]
    public void Fit_ContainsBlackBars_WhenAspectDiffers()
    {
        // Источник 100×100 (квадрат) вписывается в 200×100 → по краям чёрные поля.
        using var src = MakeSrc(100, 100, SKColors.Red);
        using var result = BackgroundFitter.FitToMonitor(src, 200, 100, WallpaperFit.Fit);

        // Центр — красный.
        result.GetPixel(100, 50).Should().Be(SKColors.Red);
        // Левый край (поле) — чёрный.
        result.GetPixel(0, 50).Should().Be(SKColors.Black);
    }

    [Fact]
    public void Center_SmallerSourceDrawnCenteredWithBlackBorder()
    {
        using var src = MakeSrc(20, 20, SKColors.Red);
        using var result = BackgroundFitter.FitToMonitor(src, 100, 100, WallpaperFit.Center);

        // Центр (40..60) — красный, углы — чёрные.
        result.GetPixel(50, 50).Should().Be(SKColors.Red);
        result.GetPixel(0, 0).Should().Be(SKColors.Black);
    }

    [Fact]
    public void Center_LargerSourceCroppedToCenter()
    {
        using var src = MakeSrc(200, 200, SKColors.Red);
        using var result = BackgroundFitter.FitToMonitor(src, 100, 100, WallpaperFit.Center);

        // Весь 100×100 — красный (центральная часть 200×200).
        result.GetPixel(0, 0).Should().Be(SKColors.Red);
        result.GetPixel(99, 99).Should().Be(SKColors.Red);
    }
}
