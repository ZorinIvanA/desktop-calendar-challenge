using DesktopCalendar.Core.Wallpaper;

namespace DesktopCalendar.Core.Tests.Wallpaper;

/// <summary>
/// Тесты GnomeUri: парсинг picture-uri (с кавычками, file://, %20, 'none'),
/// обратное построение для gsettings set, round-trip.
/// </summary>
public sealed class GnomeUriTests
{
    [Fact]
    public void Parses_SimpleAbsolutePath()
    {
        var path = GnomeUri.PathFromPictureUri("'file:///usr/share/backgrounds/wp.webp'");
        path.Should().Be("/usr/share/backgrounds/wp.webp");
    }

    [Fact]
    public void Parses_HomePath()
    {
        var path = GnomeUri.PathFromPictureUri("'file:///home/user/Pictures/x.jpg'");
        path.Should().Be("/home/user/Pictures/x.jpg");
    }

    [Fact]
    public void Parses_PercentEncodedSpaces()
    {
        var path = GnomeUri.PathFromPictureUri("'file:///home/user/My%20Photos/wp.jpg'");
        path.Should().Be("/home/user/My Photos/wp.jpg");
    }

    [Fact]
    public void Parses_NoneValue_ReturnsNull()
    {
        GnomeUri.PathFromPictureUri("'none'").Should().BeNull();
        GnomeUri.PathFromPictureUri("none").Should().BeNull();
    }

    [Fact]
    public void Parses_BareDoubleQuotes()
    {
        // gsettings также нормализует двойные кавычки — должны уметь читать.
        var path = GnomeUri.PathFromPictureUri("\"file:///tmp/x.png\"");
        path.Should().Be("/tmp/x.png");
    }

    [Fact]
    public void RoundTrip_RegularPath()
    {
        var original = "/home/user/Pictures/calendar.png";
        var gvariant = GnomeUri.ToGvariantUri(original);
        // gvariant должно начинаться с одинарной кавычки и file://
        gvariant.Should().StartWith("'file://");
        gvariant.Should().EndWith("'");

        var back = GnomeUri.PathFromPictureUri(gvariant);
        back.Should().Be(original);
    }

    [Fact]
    public void RoundTrip_PathWithSpaces()
    {
        var original = "/home/user/My Photos/calendar.png";
        var gvariant = GnomeUri.ToGvariantUri(original);
        // Должен percent-encode пробел → %20.
        gvariant.Should().Contain("%20");

        var back = GnomeUri.PathFromPictureUri(gvariant);
        back.Should().Be(original);
    }

    [Fact]
    public void ToGvariantUri_None()
    {
        GnomeUri.ToGvariantUri("none").Should().Be("'none'");
        GnomeUri.NoneGvariant().Should().Be("'none'");
    }
}
