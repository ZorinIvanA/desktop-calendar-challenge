using DesktopCalendar.Core.Calendar;
using DesktopCalendar.Core.Settings;

namespace DesktopCalendar.Core.Tests.Calendar;

/// <summary>
/// Тесты геометрии: anchor-формулы для всех 8 точек на двух разрешениях.
/// Формулы — из SPECIFICATION §5.2.
/// </summary>
public sealed class LayoutCalculatorTests
{
    private const double W = 1920;
    private const double H = 1080;
    private const int Margin = 32;
    private static readonly MeasureSize Block = new(400, 300);

    private static CalendarRect Place(Anchor a) =>
        LayoutCalculator.Place(a, Margin, W, H, Block);

    private static CalendarRect PlaceOn(Anchor a, double w, double h, int margin, MeasureSize block) =>
        LayoutCalculator.Place(a, margin, w, h, block);

    [Fact] public void TopLeft()        => Place(Anchor.TopLeft).Should().Be(new CalendarRect(32, 32, 400, 300));
    [Fact] public void TopCenter()      => Place(Anchor.TopCenter).Should().Be(new CalendarRect((1920 - 400) / 2, 32, 400, 300));
    [Fact] public void TopRight()       => Place(Anchor.TopRight).Should().Be(new CalendarRect(1920 - 400 - 32, 32, 400, 300));
    [Fact] public void CenterLeft()     => Place(Anchor.CenterLeft).Should().Be(new CalendarRect(32, (1080 - 300) / 2, 400, 300));
    [Fact] public void CenterRight()    => Place(Anchor.CenterRight).Should().Be(new CalendarRect(1920 - 400 - 32, (1080 - 300) / 2, 400, 300));
    [Fact] public void BottomLeft()     => Place(Anchor.BottomLeft).Should().Be(new CalendarRect(32, 1080 - 300 - 32, 400, 300));
    [Fact] public void BottomCenter()   => Place(Anchor.BottomCenter).Should().Be(new CalendarRect((1920 - 400) / 2, 1080 - 300 - 32, 400, 300));
    [Fact] public void BottomRight()    => Place(Anchor.BottomRight).Should().Be(new CalendarRect(1920 - 400 - 32, 1080 - 300 - 32, 400, 300));

    [Fact]
    public void MarginZero_TopLeft_AtOrigin()
    {
        var r = PlaceOn(Anchor.TopLeft, 1920, 1080, 0, Block);
        r.X.Should().Be(0);
        r.Y.Should().Be(0);
    }

    [Fact]
    public void MarginZero_BottomRight_AtFarCorner()
    {
        var r = PlaceOn(Anchor.BottomRight, 2560, 1440, 0, Block);
        r.X.Should().Be(2560 - 400);
        r.Y.Should().Be(1440 - 300);
    }

    [Fact]
    public void CenterAnchors_CenterOnRespectiveAxis()
    {
        var topCenter = Place(Anchor.TopCenter);
        topCenter.X.Should().Be((1920 - 400) / 2);
        topCenter.Y.Should().Be(Margin); // не центр по Y

        var centerLeft = Place(Anchor.CenterLeft);
        centerLeft.Y.Should().Be((1080 - 300) / 2);
        centerLeft.X.Should().Be(Margin); // не центр по X
    }

    [Fact]
    public void Resolution2560x1440_FormulasScale()
    {
        var br = PlaceOn(Anchor.BottomRight, 2560, 1440, 50, Block);
        br.X.Should().Be(2560 - 400 - 50);
        br.Y.Should().Be(1440 - 300 - 50);
    }

    [Fact]
    public void RectStaysWithinCanvas_WithMargin()
    {
        foreach (Anchor a in Enum.GetValues<Anchor>())
        {
            var r = Place(a);
            r.X.Should().BeInRange(0, W - r.Width, $"anchor {a}: X в пределах");
            r.Y.Should().BeInRange(0, H - r.Height, $"anchor {a}: Y в пределах");
            (r.X + r.Width).Should().BeLessThanOrEqualTo(W, $"anchor {a}: правый край в пределах");
            (r.Y + r.Height).Should().BeLessThanOrEqualTo(H, $"anchor {a}: нижний край в пределах");
        }
    }
}
