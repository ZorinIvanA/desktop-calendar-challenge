using DesktopCalendar.Core.Settings;
using Microsoft.Extensions.Logging.Abstractions;

namespace DesktopCalendar.Core.Tests.Settings;

/// <summary>
/// Тесты DebouncedSettingsStore: Flush() форсирует запись отложенного значения,
/// Load() делегируется внутреннему store, Dispose() тоже флашит.
/// </summary>
public sealed class DebouncedSettingsStoreTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _settingsPath;

    public DebouncedSettingsStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "dcc-debounce-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _settingsPath = Path.Combine(_tempDir, "settings.json");
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* ignore */ }
    }

    private DebouncedSettingsStore CreateStore(TimeSpan? delay = null) =>
        new(new JsonSettingsStore(_settingsPath, NullLogger<JsonSettingsStore>.Instance),
            delay ?? TimeSpan.FromMilliseconds(500),
            NullLogger<DebouncedSettingsStore>.Instance);

    [Fact]
    public void Flush_WritesPendingValueImmediately()
    {
        using var store = CreateStore(TimeSpan.FromSeconds(10));
        var settings = new AppSettings { MarginPx = 77 };

        store.Save(settings);
        File.Exists(_settingsPath).Should().BeFalse("запись отложена");

        store.Flush();

        File.Exists(_settingsPath).Should().BeTrue();
        store.Load().MarginPx.Should().Be(77);
    }

    [Fact]
    public void Load_DelegatesToInnerStore()
    {
        using var store = CreateStore();
        store.Save(new AppSettings { TargetMonitorId = "DELEGATE" });
        store.Flush();

        store.Load().TargetMonitorId.Should().Be("DELEGATE");
    }

    [Fact]
    public void Dispose_FlushesPendingValue()
    {
        DebouncedSettingsStore store;
        using (store = CreateStore(TimeSpan.FromSeconds(10)))
        {
            store.Save(new AppSettings { FontFamily = "TestFont" });
        } // Dispose здесь.

        var inner = new JsonSettingsStore(_settingsPath, NullLogger<JsonSettingsStore>.Instance);
        inner.Load().FontFamily.Should().Be("TestFont");
    }
}
