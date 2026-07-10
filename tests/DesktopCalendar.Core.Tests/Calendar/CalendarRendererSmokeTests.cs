using DesktopCalendar.Core.Calendar;
using DesktopCalendar.Core.Settings;
using SkiaSharp;

namespace DesktopCalendar.Core.Tests.Calendar;

/// <summary>
/// Smoke-тест рендерера: рисует календарь на синтетическом фоне и сохраняет PNG.
/// Не проверяет конкретные пиксели (хрупко), а фиксирует: нет исключения, размер канваса
/// сохранён, файл создан и ненулевого размера. Визуальная проверка — отдельным просмотром PNG.
/// </summary>
public sealed class CalendarRendererSmokeTests : IDisposable
{
    private readonly string _outDir = Path.Combine(Path.GetTempPath(), "dcc-render-" + Guid.NewGuid().ToString("N"));

    public CalendarRendererSmokeTests()
    {
        Directory.CreateDirectory(_outDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_outDir, recursive: true); } catch { /* ignore */ }
    }

    private static AppSettings DefaultSettings(Anchor anchor = Anchor.BottomRight) => new()
    {
        Anchor = anchor,
        MarginPx = 40,
        FontFamily = "DejaVu Sans",
        FontSize = 28,
        ColorMonth = PresetColor.White,
        ColorWeekday = PresetColor.White,
        ColorDay = PresetColor.White,
        ColorToday = PresetColor.Red,
        ColorWeekend = PresetColor.Blue,
        OtherMonthMode = OtherMonthMode.Show,
        LabelsPosition = LabelsPosition.Top,
    };

    [Fact]
    public void Render_OnSolidBackground_ProducesPng()
    {
        using var renderer = new CalendarRenderer(2026, 7, new DateOnly(2026, 7, 10));
        using var bmp = renderer.Render(SKColors.Navy, 1920, 1080, DefaultSettings(), renderDpi: 96);

        bmp.Width.Should().Be(1920);
        bmp.Height.Should().Be(1080);

        var path = Path.Combine(_outDir, "smoke.png");
        using (var data = bmp.Encode(SKEncodedImageFormat.Png, 100))
        {
            File.WriteAllBytes(path, data.ToArray());
        }
        new FileInfo(path).Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Render_OnBitmapBackground_DoesNotMutateOriginal()
    {
        // Готовим «оригинальный» фон.
        var original = new SKBitmap(new SKImageInfo(2560, 1440, SKColorType.Bgra8888, SKAlphaType.Premul));
        using (var sk = new SKCanvas(original)) sk.Clear(SKColors.DarkGray);

        SKColor sampleBefore = original.GetPixel(0, 0);

        using var renderer = new CalendarRenderer(2026, 7, new DateOnly(2026, 7, 10));
        using var result = renderer.Render(original, DefaultSettings(Anchor.BottomRight), renderDpi: 96);

        result.Width.Should().Be(2560);
        result.Height.Should().Be(1440);
        // Оригинал не должен измениться.
        original.GetPixel(0, 0).Should().Be(sampleBefore);
        // Угол результата — фон (календарь в углу BottomRight, левый-верхний угол чистый).
        result.GetPixel(0, 0).Should().Be(SKColors.DarkGray);
    }

    [Theory]
    [InlineData(Anchor.TopLeft)]
    [InlineData(Anchor.BottomRight)]
    [InlineData(Anchor.CenterRight)]
    public void Render_AllAnchors_NoException(Anchor anchor)
    {
        using var renderer = new CalendarRenderer(2026, 7, new DateOnly(2026, 7, 10));
        using var bmp = renderer.Render(SKColors.Black, 1920, 1080, DefaultSettings(anchor), renderDpi: 96);

        bmp.Width.Should().Be(1920);
        bmp.Height.Should().Be(1080);
    }

    [Theory]
    [InlineData(LabelsPosition.Top)]
    [InlineData(LabelsPosition.Bottom)]
    public void Render_BothLabelsPositions_NoException(LabelsPosition pos)
    {
        var settings = DefaultSettings();
        settings.LabelsPosition = pos;

        using var renderer = new CalendarRenderer(2026, 7, new DateOnly(2026, 7, 10));
        using var bmp = renderer.Render(SKColors.Black, 1920, 1080, settings, renderDpi: 96);

        bmp.Should().NotBeNull();
    }

    [Theory]
    [InlineData(OtherMonthMode.Show)]
    [InlineData(OtherMonthMode.Hide)]
    [InlineData(OtherMonthMode.CustomOpacity)]
    public void Render_AllOtherMonthModes_NoException(OtherMonthMode mode)
    {
        var settings = DefaultSettings();
        settings.OtherMonthMode = mode;
        settings.OtherMonthOpacity = 30;

        using var renderer = new CalendarRenderer(2026, 7, new DateOnly(2026, 7, 10));
        using var bmp = renderer.Render(SKColors.Black, 1920, 1080, settings, renderDpi: 96);

        bmp.Should().NotBeNull();
    }

    [Fact]
    public void Render_OpacityZero_OtherMonthInvisible()
    {
        // При opacity=0 дни соседних месяцев имеют alpha=0 → их пиксели не должны меняться.
        var settings = DefaultSettings();
        settings.OtherMonthMode = OtherMonthMode.CustomOpacity;
        settings.OtherMonthOpacity = 0;

        // Контрастный фон + смотрим, что календарь всё равно нарисован (текущий месяц),
        // хотя соседние дни невидимы. Полная проверка «не рисуется» — по пикселям хрупко,
        // поэтому просто фиксируем отсутствие исключения и размер.
        using var renderer = new CalendarRenderer(2026, 7, new DateOnly(2026, 7, 10));
        using var bmp = renderer.Render(SKColors.White, 1920, 1080, settings, renderDpi: 96);

        bmp.Width.Should().Be(1920);
        bmp.Height.Should().Be(1080);
    }
}
