using DesktopCalendar.Core.Contracts;
using DesktopCalendar.Core.Wallpaper;

namespace DesktopCalendar.Core.Tests.Wallpaper;

/// <summary>
/// Тесты маппинга WallpaperFit ↔ Windows DWPOS / GNOME picture-options.
/// </summary>
public sealed class WallpaperFitMappingTests
{
    [Theory]
    [InlineData(WallpaperFit.Center, 0)]
    [InlineData(WallpaperFit.Tile, 1)]
    [InlineData(WallpaperFit.Stretch, 2)]
    [InlineData(WallpaperFit.Fit, 3)]
    [InlineData(WallpaperFit.Fill, 4)]
    [InlineData(WallpaperFit.Span, 5)]
    public void Windows_Position_RoundTrip(WallpaperFit fit, int position)
    {
        WallpaperFitMapping.ToWindowsPosition(fit).Should().Be(position);
        WallpaperFitMapping.FromWindowsPosition(position).Should().Be(fit);
    }

    [Theory]
    [InlineData(WallpaperFit.None, "none")]
    [InlineData(WallpaperFit.Tile, "wallpaper")]
    [InlineData(WallpaperFit.Center, "centered")]
    [InlineData(WallpaperFit.Fit, "scaled")]
    [InlineData(WallpaperFit.Stretch, "stretched")]
    [InlineData(WallpaperFit.Fill, "zoom")]
    [InlineData(WallpaperFit.Span, "spanned")]
    public void Gnome_Options_RoundTrip(WallpaperFit fit, string options)
    {
        WallpaperFitMapping.ToGnomeOptions(fit).Should().Be(options);
        WallpaperFitMapping.FromGnomeOptions(options).Should().Be(fit);
    }

    [Fact]
    public void Gnome_Options_StripsGvariantQuotes()
    {
        // gsettings get возвращает значение в одинарных кавычках.
        WallpaperFitMapping.FromGnomeOptions("'zoom'").Should().Be(WallpaperFit.Fill);
    }

    [Fact]
    public void UnknownWindowsPosition_DefaultsCenter()
    {
        WallpaperFitMapping.FromWindowsPosition(999).Should().Be(WallpaperFit.Center);
    }

    [Fact]
    public void UnknownGnomeOptions_DefaultsCenter()
    {
        WallpaperFitMapping.FromGnomeOptions("garbage").Should().Be(WallpaperFit.Center);
    }
}
