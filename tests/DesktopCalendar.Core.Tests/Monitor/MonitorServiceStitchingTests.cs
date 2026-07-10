using DesktopCalendar.Core.Platform;

namespace DesktopCalendar.Core.Tests.Monitor;

/// <summary>
/// Тесты композитора MonitorService: сшивка identity и geometry по пересечению bounds,
/// degraded-режим без identity, сортировка, обработка исчезнувшего монитора.
/// </summary>
public sealed class MonitorServiceStitchingTests
{
    // Хелперы: создаём геометрию и identity с прямыугольниками.
    private static ScreenGeometry Geo(int x, int y, int w, int h, bool primary = false, string? name = null)
        => new(new ScreenRect(x, y, w, h), new ScreenRect(x, y, w, h), Scaling: 1.0, IsPrimary: primary, DisplayName: name);

    private static MonitorIdentity Ident(string id, int x, int y, int w, int h, int idx, string? connector = null)
        => new(id, connector, DisplayName: null, LogicalIndex: idx, Bounds: new ScreenRect(x, y, w, h));

    private sealed class FakeGeometry : IGeometryProvider
    {
        private readonly List<ScreenGeometry> _screens;
        public FakeGeometry(params ScreenGeometry[] screens) => _screens = screens.ToList();
        public IReadOnlyList<ScreenGeometry> GetScreens() => _screens;
    }

    private sealed class FakeIdentity : IMonitorIdentityProvider
    {
        private readonly List<MonitorIdentity> _ids;
        public FakeIdentity(params MonitorIdentity[] ids) => _ids = ids.ToList();
        public IReadOnlyList<MonitorIdentity> GetIdentities() => _ids;
    }

    [Fact]
    public void Degraded_WithoutIdentity_UsesDisplayNameAsId_SortedByPosition()
    {
        var svc = new MonitorService(
            new FakeGeometry(
                Geo(-1920, 0, 1920, 1080, name: "Secondary"),
                Geo(0, 0, 2560, 1440, primary: true, name: "Primary")),
            identity: null);

        var monitors = svc.GetMonitors();

        monitors.Should().HaveCount(2);
        // Сортировка по X,Y: левый (-1920) — первый.
        monitors[0].LogicalIndex.Should().Be(1);
        monitors[0].BoundsX.Should().Be(-1920);
        monitors[1].LogicalIndex.Should().Be(2);
        monitors[1].IsPrimary.Should().BeTrue();
    }

    [Fact]
    public void Stitches_IdentityToGeometry_ByMaxIntersection()
    {
        var svc = new MonitorService(
            new FakeGeometry(
                Geo(0, 0, 2560, 1440, primary: true),       // Avalonia screen A
                Geo(2560, 0, 1920, 1080)),                    // Avalonia screen B
            new FakeIdentity(
                Ident("GSM:abc:1234", 0, 0, 2560, 1440, idx: 1, connector: "DP-1"),
                Ident("SAM:xyz:5678", 2560, 0, 1920, 1080, idx: 2, connector: "HDMI-1")));

        var monitors = svc.GetMonitors();

        monitors.Should().HaveCount(2);
        monitors[0].Id.Should().Be("GSM:abc:1234");
        monitors[0].LogicalIndex.Should().Be(1);
        monitors[0].BoundsWidth.Should().Be(2560);
        monitors[0].IsPrimary.Should().BeTrue();
        monitors[1].Id.Should().Be("SAM:xyz:5678");
        monitors[1].LogicalIndex.Should().Be(2);
    }

    [Fact]
    public void Stitches_EvenWhenBoundsDontExactlyMatch_PicksMaxOverlap()
    {
        // Платформенный API может сообщать слегка отличающиеся bounds (напр. на Wayland logical vs device px).
        //identity: DP-1 примерно по (0,0) 2560x1440, но смещён.
        var svc = new MonitorService(
            new FakeGeometry(Geo(0, 0, 2560, 1440)),
            new FakeIdentity(Ident("GSM:abc:1234", 0, 0, 2500, 1400, idx: 1, connector: "DP-1")));

        var monitors = svc.GetMonitors();

        monitors.Should().ContainSingle().Which.Id.Should().Be("GSM:abc:1234");
    }

    [Fact]
    public void IdentityWithoutGeometry_MonitorDisconnected_IsSkipped()
    {
        var svc = new MonitorService(
            new FakeGeometry(Geo(0, 0, 2560, 1440, primary: true)),  // только один экран физически
            new FakeIdentity(
                Ident("GSM:abc:1234", 0, 0, 2560, 1440, idx: 1, connector: "DP-1"),
                Ident("SAM:xyz:5678", 2560, 0, 1920, 1080, idx: 2, connector: "HDMI-1")));

        var monitors = svc.GetMonitors();

        monitors.Should().ContainSingle();
        monitors[0].Id.Should().Be("GSM:abc:1234");
    }

    [Fact]
    public void FriendlyName_IncludesConnectorAndDisplayName()
    {
        var svc = new MonitorService(
            new FakeGeometry(Geo(0, 0, 2560, 1440, primary: true, name: "LG Ultra HD")),
            new FakeIdentity(new MonitorIdentity(
                Id: "GSM:abc:1234",
                Connector: "DP-1",
                DisplayName: "LG Ultra HD",
                LogicalIndex: 1,
                Bounds: new ScreenRect(0, 0, 2560, 1440))));

        var monitor = svc.GetMonitors().Single();

        monitor.FriendlyName.Should().Contain("DP-1");
        monitor.FriendlyName.Should().Contain("LG Ultra HD");
        monitor.FriendlyName.Should().StartWith("1 — ");
    }

    [Fact]
    public void GetById_ReturnsMatchingOrNull()
    {
        var svc = new MonitorService(
            new FakeGeometry(Geo(0, 0, 2560, 1440, primary: true)),
            new FakeIdentity(Ident("GSM:abc:1234", 0, 0, 2560, 1440, idx: 1, connector: "DP-1")));

        svc.GetById("GSM:abc:1234").Should().NotBeNull();
        svc.GetById("nonexistent").Should().BeNull();
    }

    [Fact]
    public void PreservesOrder_FromIdentityLogicalIndex()
    {
        var svc = new MonitorService(
            new FakeGeometry(
                Geo(0, 0, 2560, 1440, primary: true),
                Geo(2560, 0, 1920, 1080)),
            new FakeIdentity(
                // намеренно перепутанный порядок в источнике, но LogicalIndex задаёт правильный.
                Ident("SAM:xyz:5678", 2560, 0, 1920, 1080, idx: 2, connector: "HDMI-1"),
                Ident("GSM:abc:1234", 0, 0, 2560, 1440, idx: 1, connector: "DP-1")));

        var monitors = svc.GetMonitors();

        monitors[0].LogicalIndex.Should().Be(1);
        monitors[0].Id.Should().Be("GSM:abc:1234");
        monitors[1].LogicalIndex.Should().Be(2);
    }
}
