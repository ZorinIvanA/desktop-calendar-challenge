using DesktopCalendar.Core.Contracts;
using DesktopCalendar.Core.Settings;
using DesktopCalendar.Core.Wallpaper;
using Microsoft.Extensions.Logging.Abstractions;

namespace DesktopCalendar.Core.Tests.Wallpaper;

/// <summary>
/// Тесты AutoUpdateService: коды возврата, идемпотентность.
/// WallpaperApplier подменяется наследником с подсчётом вызовов Apply.
/// </summary>
public sealed class AutoUpdateServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _settingsFile;
    private readonly AppSettings _settings;

    public AutoUpdateServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "dcc-auto-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _settingsFile = Path.Combine(_tempDir, "settings.json");
        _settings = new AppSettings { TargetMonitorId = "MON1" };
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* ignore */ }
    }

    private AutoUpdateService CreateService(
        AppSettings? settings = null,
        string? monitorIdAvailable = "MON1",
        WallpaperSnapshot? snapshot = null,
        CountingApplier? applier = null)
    {
        settings ??= _settings;
        var store = new JsonSettingsStore(_settingsFile, NullLogger<JsonSettingsStore>.Instance);
        store.Save(settings);

        var wallpaper = new FakeWallpaperService(snapshot ?? new WallpaperSnapshot("", null, false));
        var monitors = new FakeMonitorService(monitorIdAvailable);
        var svc = new AutoUpdateService(store, wallpaper, monitors, applier ?? new CountingApplier(),
            NullLogger<AutoUpdateService>.Instance);
        return svc;
    }

    [Fact]
    public void NoTargetMonitor_Returns2()
    {
        var svc = CreateService(settings: new AppSettings { TargetMonitorId = null });
        svc.Run().Should().Be(AutoUpdateService.ExitNoMonitor);
    }

    [Fact]
    public void NoMonitorResolution_Returns3()
    {
        // Монитор выбран, но разрешение не сохранено (silent без UI не сможет рендерить).
        var svc = CreateService(settings: new AppSettings
        {
            TargetMonitorId = "MON1",
            LastMonitorWidth = null,
            LastMonitorHeight = null,
        });
        svc.Run().Should().Be(AutoUpdateService.ExitMonitorGone);
    }

    [Fact]
    public void FreshRun_AppliesAndReturns0()
    {
        var applier = new CountingApplier();
        var settings = new AppSettings { TargetMonitorId = "MON1", LastMonitorWidth = 320, LastMonitorHeight = 200 };
        var svc = CreateService(settings: settings, applier: applier);

        svc.Run().Should().Be(AutoUpdateService.ExitOk);
        applier.ApplyCalls.Should().Be(1);
    }

    [Fact]
    public void Idempotent_SameDaySameWallpaper_NoApply_Returns0()
    {
        var generatedPath = "/tmp/generated.png";
        var settings = new AppSettings
        {
            TargetMonitorId = "MON1", LastMonitorWidth = 320, LastMonitorHeight = 200,
            LastAppliedUtc = DateTime.UtcNow, // сегодня UTC
            LastGeneratedPath = generatedPath,
        };
        var snapshot = new WallpaperSnapshot(generatedPath, generatedPath, HasCalendar: true);
        var applier = new CountingApplier();
        var svc = CreateService(settings: settings, snapshot: snapshot, applier: applier);

        svc.Run().Should().Be(AutoUpdateService.ExitOk);
        applier.ApplyCalls.Should().Be(0, "идемпотентный no-op: сегодня уже применено");
    }

    [Fact]
    public void DifferentDay_Reapplies_Returns0()
    {
        var generatedPath = "/tmp/generated.png";
        var settings = new AppSettings
        {
            TargetMonitorId = "MON1", LastMonitorWidth = 320, LastMonitorHeight = 200,
            LastAppliedUtc = DateTime.UtcNow.AddDays(-1), // вчера
            LastGeneratedPath = generatedPath,
        };
        var snapshot = new WallpaperSnapshot(generatedPath, generatedPath, HasCalendar: true);
        var applier = new CountingApplier();
        var svc = CreateService(settings: settings, snapshot: snapshot, applier: applier);

        svc.Run().Should().Be(AutoUpdateService.ExitOk);
        applier.ApplyCalls.Should().Be(1, "новый день → перерисовка");
    }

    [Fact]
    public void UserChangedWallpaper_Reapplies_Returns0()
    {
        // Сегодня применяли, но пользователь сменил обои → currentUri != LastGeneratedPath → перерисуем.
        var generatedPath = "/tmp/generated.png";
        var settings = new AppSettings
        {
            TargetMonitorId = "MON1", LastMonitorWidth = 320, LastMonitorHeight = 200,
            LastAppliedUtc = DateTime.UtcNow,
            LastGeneratedPath = generatedPath,
        };
        var snapshot = new WallpaperSnapshot("/tmp/user-new-wallpaper.jpg", null, HasCalendar: false);
        var applier = new CountingApplier();
        var svc = CreateService(settings: settings, snapshot: snapshot, applier: applier);

        svc.Run().Should().Be(AutoUpdateService.ExitOk);
        applier.ApplyCalls.Should().Be(1, "обои сменились → перерисовка");
    }

    [Fact]
    public void NeverApplied_Applies_Returns0()
    {
        var settings = new AppSettings
        {
            TargetMonitorId = "MON1", LastMonitorWidth = 320, LastMonitorHeight = 200,
            LastAppliedUtc = null,
        };
        var applier = new CountingApplier();
        var svc = CreateService(settings: settings, applier: applier);

        svc.Run().Should().Be(AutoUpdateService.ExitOk);
        applier.ApplyCalls.Should().Be(1);
    }

    // --- фейки ---

    private sealed class CountingApplier : WallpaperApplier
    {
        public int ApplyCalls;
        public CountingApplier() : base(null!, null!, null!, NullLogger<WallpaperApplier>.Instance) { }
        public override void Apply(string monitorId, AppSettings settings, DateOnly today, double renderDpi = 96.0)
            => ApplyCalls++;
    }

    private sealed class FakeWallpaperService : IWallpaperService
    {
        private readonly WallpaperSnapshot _snapshot;
        public FakeWallpaperService(WallpaperSnapshot snapshot) => _snapshot = snapshot;
        public WallpaperSnapshot GetCurrent(string monitorId) => _snapshot;
        public void SetWallpaper(string monitorId, string imageFilePath) { }
        public string? GetOriginalPath(string monitorId) => null;
        public WallpaperFit ParseCurrentFit(string monitorId) => WallpaperFit.Fill;
        public void SetFit(string monitorId, WallpaperFit fit) { }
    }

    private sealed class FakeMonitorService : IMonitorService
    {
        private readonly string? _availableId;
        public FakeMonitorService(string? availableId) => _availableId = availableId;
        public IReadOnlyList<MonitorInfo> GetMonitors()
            => _availableId is null ? Array.Empty<MonitorInfo>() : new[] { MakeInfo(_availableId) };
        public MonitorInfo? GetById(string id) => id == _availableId ? MakeInfo(id) : null;
        private static MonitorInfo MakeInfo(string id) =>
            new(id, 1, "T", 0, 0, 320, 200, 320, 200, true);
    }
}
