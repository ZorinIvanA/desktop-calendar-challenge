using DesktopCalendar.Core.Contracts;
using DesktopCalendar.Core.Settings;
using Microsoft.Extensions.Logging.Abstractions;

namespace DesktopCalendar.Core.Tests.Settings;

/// <summary>
/// Тесты JsonSettingsStore: round-trip всех полей, дефолты при отсутствии файла,
/// устойчивость к повреждённому JSON.
/// </summary>
public sealed class JsonSettingsStoreTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _settingsPath;

    public JsonSettingsStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "dcc-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _settingsPath = Path.Combine(_tempDir, "settings.json");
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* ignore */ }
    }

    private JsonSettingsStore CreateStore() =>
        new(_settingsPath, NullLogger<JsonSettingsStore>.Instance);

    [Fact]
    public void Load_WhenFileMissing_ReturnsDefaults()
    {
        var store = CreateStore();

        var loaded = store.Load();

        loaded.Should().NotBeNull();
        loaded.Anchor.Should().Be(Anchor.BottomRight);
        loaded.MarginPx.Should().Be(32);
        loaded.FontFamily.Should().Be("Segoe UI");
        loaded.FontSize.Should().Be(28);
        loaded.ColorMonth.Should().Be(PresetColor.White);
        loaded.ColorToday.Should().BeNull();
        loaded.ColorWeekend.Should().BeNull();
        loaded.OtherMonthMode.Should().Be(OtherMonthMode.Show);
        loaded.OtherMonthOpacity.Should().Be(100);
        loaded.LabelsPosition.Should().Be(LabelsPosition.Top);
        loaded.Autorun.Should().BeFalse();
        loaded.TargetMonitorId.Should().BeNull();
    }

    [Fact]
    public void SaveThenLoad_RoundTripsAllFields()
    {
        var store = CreateStore();
        var original = new AppSettings
        {
            TargetMonitorId = "MONITOR-XYZ",
            Anchor = Anchor.TopLeft,
            MarginPx = 80,
            FontFamily = "Comic Sans MS",
            FontSize = 42.5,
            ColorMonth = PresetColor.Red,
            ColorWeekday = PresetColor.Green,
            ColorDay = PresetColor.Blue,
            ColorToday = PresetColor.Red,
            ColorWeekend = PresetColor.Green,
            OtherMonthMode = OtherMonthMode.CustomOpacity,
            OtherMonthOpacity = 35,
            LabelsPosition = LabelsPosition.Bottom,
            Autorun = true,
            LastOriginalPath = "/tmp/original.png",
            LastGeneratedPath = "/tmp/generated.png",
            LastAppliedUtc = new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc),
            OriginalFit = WallpaperFit.Center,
        };

        store.Save(original);
        var loaded = store.Load();

        loaded.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void Load_WhenFileCorrupt_ReturnsDefaults()
    {
        File.WriteAllText(_settingsPath, "{ this is not valid json }}}");
        var store = CreateStore();

        var loaded = store.Load();

        loaded.Anchor.Should().Be(Anchor.BottomRight);
        loaded.TargetMonitorId.Should().BeNull();
    }

    [Fact]
    public void Save_CreatesParentDirectoryIfMissing()
    {
        var nestedPath = Path.Combine(_tempDir, "nested", "deep", "settings.json");
        var store = new JsonSettingsStore(nestedPath, NullLogger<JsonSettingsStore>.Instance);

        var settings = new AppSettings { TargetMonitorId = "ABC" };
        var act = () => store.Save(settings);

        act.Should().NotThrow();
        File.Exists(nestedPath).Should().BeTrue();
    }

    [Fact]
    public void Save_OverwritesPreviousValue()
    {
        var store = CreateStore();
        store.Save(new AppSettings { MarginPx = 10 });
        store.Save(new AppSettings { MarginPx = 99 });

        var loaded = store.Load();

        loaded.MarginPx.Should().Be(99);
    }
}
