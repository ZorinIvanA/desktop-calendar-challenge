using DesktopCalendar.Core.Settings;
using Microsoft.Extensions.Logging.Abstractions;

namespace DesktopCalendar.Core.Tests.Settings;

/// <summary>
/// Интеграция Persist() → JSON-файл. В M1 MainViewModel живёт в UI-проекте (нужен Avalonia),
/// поэтому тестируем эквивалентный путь: AppSettings → DebouncedSettingsStore → settings.json.
/// В M5, когда VM переедет в тестируемое место, покроем Persist() напрямую.
/// </summary>
public sealed class SettingsPipelineTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _settingsPath;

    public SettingsPipelineTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "dcc-pipe-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _settingsPath = Path.Combine(_tempDir, "settings.json");
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* ignore */ }
    }

    [Fact]
    public void Persist_ThroughDebouncedStore_WritesJsonToFile()
    {
        var inner = new JsonSettingsStore(_settingsPath, NullLogger<JsonSettingsStore>.Instance);
        using var debounced = new DebouncedSettingsStore(
            inner, TimeSpan.FromMilliseconds(500), NullLogger<DebouncedSettingsStore>.Instance);

        var settings = new AppSettings
        {
            TargetMonitorId = "DP-2",
            Anchor = Anchor.BottomRight,
            MarginPx = 64,
            Autorun = true,
        };

        // Имитация MainViewModel.Persist(): Save() + Flush().
        debounced.Save(settings);
        debounced.Flush();

        File.Exists(_settingsPath).Should().BeTrue();
        var reloaded = debounced.Load();
        reloaded.Should().BeEquivalentTo(settings);
        reloaded.TargetMonitorId.Should().Be("DP-2");
        reloaded.Autorun.Should().BeTrue();
    }
}
