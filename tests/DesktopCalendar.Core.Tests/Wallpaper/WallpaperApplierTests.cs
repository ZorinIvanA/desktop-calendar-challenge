using DesktopCalendar.Core.Calendar;
using DesktopCalendar.Core.Contracts;
using DesktopCalendar.Core.Settings;
using DesktopCalendar.Core.Wallpaper;
using Microsoft.Extensions.Logging.Abstractions;
using SkiaSharp;

namespace DesktopCalendar.Core.Tests.Wallpaper;

/// <summary>
/// Тесты WallpaperApplier с фейковым IWallpaperService и IPlatformPaths во temp-каталоге.
/// Без обращения к реальным обоям ОС. Проверки: apply создаёт файл и HasCalendar,
/// повторный apply удаляет старый, remove восстанавливает оригинал.
/// </summary>
public sealed class WallpaperApplierTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _generatedDir;
    private readonly string _originalsDir;
    private readonly FakeWallpaper _wallpaper;
    private readonly WallpaperApplier _applier;
    private readonly AppSettings _settings;

    public WallpaperApplierTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "dcc-apply-" + Guid.NewGuid().ToString("N"));
        _generatedDir = Path.Combine(_tempDir, "generated");
        _originalsDir = Path.Combine(_tempDir, "originals");
        Directory.CreateDirectory(_generatedDir);
        Directory.CreateDirectory(_originalsDir);

        // Фейковый "текущий фон пользователя" — положим файл, который апликатор сохранит как оригинал.
        var userBg = Path.Combine(_tempDir, "user-bg.png");
        SaveSolidPng(userBg, 800, 600, SKColors.Coral);
        _wallpaper = new FakeWallpaper(userBg);

        var monitors = new FakeMonitors(new MonitorInfo(
            Id: "MON1", LogicalIndex: 1, FriendlyName: "Test",
            BoundsX: 0, BoundsY: 0, BoundsWidth: 320, BoundsHeight: 200,
            ResolutionWidth: 320, ResolutionHeight: 200, IsPrimary: true));

        var paths = new FakePaths(_generatedDir, _originalsDir);
        _applier = new WallpaperApplier(_wallpaper, monitors, paths, NullLogger<WallpaperApplier>.Instance);
        _settings = new AppSettings
        {
            TargetMonitorId = "MON1",
            Anchor = Anchor.BottomRight,
            MarginPx = 8,
            FontFamily = "DejaVu Sans",
            FontSize = 12,
            ColorMonth = PresetColor.White,
            ColorWeekday = PresetColor.White,
            ColorDay = PresetColor.White,
        };
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* ignore */ }
    }

    [Fact]
    public void Apply_CreatesGeneratedFile_AndSetsAsWallpaper_AndHasCalendarTrue()
    {
        _applier.Apply("MON1", _settings, new DateOnly(2026, 7, 10));

        _settings.LastGeneratedPath.Should().NotBeNull();
        File.Exists(_settings.LastGeneratedPath).Should().BeTrue();

        _wallpaper.SetWallpaperCalled.Should().Be(1);
        _wallpaper.LastSetPath.Should().Be(_settings.LastGeneratedPath);
        _wallpaper.LastSetFit.Should().Be(WallpaperFit.Center);

        // Текущий фон теперь — наш файл → HasCalendar true.
        _wallpaper.GetCurrent("MON1").HasCalendar.Should().BeTrue();
    }

    [Fact]
    public void Apply_SavesOriginal_AndRestoreOnRemove()
    {
        _applier.Apply("MON1", _settings, new DateOnly(2026, 7, 10));

        // Оригинал должен быть сохранён.
        _settings.LastOriginalPath.Should().NotBeNull();
        File.Exists(_settings.LastOriginalPath).Should().BeTrue();
        var savedFit = _settings.OriginalFit;
        savedFit.Should().NotBeNull();

        // Remove возвращает оригинал.
        _applier.Remove("MON1", _settings);

        _wallpaper.LastSetPath.Should().Be(_settings.LastOriginalPath);
        _wallpaper.LastSetFit.Should().Be(savedFit!.Value);
        _settings.LastGeneratedPath.Should().BeNull();
    }

    [Fact]
    public void SecondApply_DeletesOldGeneratedFile()
    {
        _applier.Apply("MON1", _settings, new DateOnly(2026, 7, 10));
        var firstGenerated = _settings.LastGeneratedPath!;
        File.Exists(firstGenerated).Should().BeTrue();

        // Теперь текущий фон — наш файл. Apply ещё раз.
        _applier.Apply("MON1", _settings, new DateOnly(2026, 7, 10));

        // Старый файл должен быть удалён.
        File.Exists(firstGenerated).Should().BeFalse("старый сгенерированный файл удаляется");
        // Новый — на месте.
        File.Exists(_settings.LastGeneratedPath).Should().BeTrue();
        // В каталоге ровно один сгенерированный файл этого монитора.
        var remaining = Directory.EnumerateFiles(_generatedDir, "*_cal_*");
        remaining.Should().ContainSingle();
    }

    [Fact]
    public void Apply_UpdatesLastAppliedUtc()
    {
        _settings.LastAppliedUtc.Should().BeNull();

        _applier.Apply("MON1", _settings, new DateOnly(2026, 7, 10));

        _settings.LastAppliedUtc.Should().NotBeNull();
        var age = DateTime.UtcNow - _settings.LastAppliedUtc!.Value;
        age.Should().BeLessThan(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Apply_WithNoneOriginal_CreatesSolidFallback()
    {
        // Фон = 'none' (нет файла).
        var wallpaper = new FakeWallpaper(currentPath: "");
        var monitors = new FakeMonitors(new MonitorInfo(
            "MON1", 1, "T", 0, 0, 320, 200, 320, 200, true));
        var paths = new FakePaths(_generatedDir, _originalsDir);
        var applier = new WallpaperApplier(wallpaper, monitors, paths, NullLogger<WallpaperApplier>.Instance);
        var settings = new AppSettings
        {
            TargetMonitorId = "MON1",
            Anchor = Anchor.BottomRight,
            MarginPx = 8,
            FontFamily = "DejaVu Sans",
            FontSize = 12,
            ColorMonth = PresetColor.White,
            ColorWeekday = PresetColor.White,
            ColorDay = PresetColor.White,
        };

        var act = () => applier.Apply("MON1", settings, new DateOnly(2026, 7, 10));
        act.Should().NotThrow();
        File.Exists(settings.LastGeneratedPath).Should().BeTrue();
    }

    [Fact]
    public void Apply_AfterRemove_PicksUpNewOriginal()
    {
        // Сценарий TZ §4.8: apply → user сменил обои → remove (вернул старый оригинал) →
        // user ставит новый фон → apply подхватывает новый оригинал.
        // Пока стоит наш календарь, URI перезаписан нашим файлом — обнаружить внешнюю смену
        // можно только когда текущие обои ≠ наш файл и ≠ сохранённый оригинал (т.е. после remove).
        _applier.Apply("MON1", _settings, new DateOnly(2026, 7, 10));
        _applier.Remove("MON1", _settings);

        // Пользователь ставит новый фон.
        var newBg = Path.Combine(_tempDir, "new-user-bg.png");
        SaveSolidPng(newBg, 640, 480, SKColors.Teal);
        _wallpaper.SetCurrent(newBg);

        _applier.Apply("MON1", _settings, new DateOnly(2026, 7, 10));

        // Теперь LastOriginalPath указывает на свежесохранённый новый фон.
        _settings.LastOriginalPath.Should().NotBeNull();
        File.Exists(_settings.LastOriginalPath).Should().BeTrue();
    }

    [Fact]
    public void Apply_OnUnknownMonitor_Throws()
    {
        var act = () => _applier.Apply("NONEXISTENT", _settings, new DateOnly(2026, 7, 10));
        act.Should().Throw<InvalidOperationException>();
    }

    private static void SaveSolidPng(string path, int w, int h, SKColor color)
    {
        using var bmp = new SKBitmap(new SKImageInfo(w, h, SKColorType.Bgra8888, SKAlphaType.Premul));
        using (var sk = new SKCanvas(bmp)) sk.Clear(color);
        using var data = bmp.Encode(SKEncodedImageFormat.Png, 100);
        File.WriteAllBytes(path, data.ToArray());
    }

    // --- фейки ---

    private sealed class FakeWallpaper : IWallpaperService
    {
        private string _currentPath;
        public int SetWallpaperCalled;
        public string? LastSetPath;
        public WallpaperFit? LastSetFit;

        public FakeWallpaper(string currentPath) => _currentPath = currentPath;

        public void SetCurrent(string path) => _currentPath = path;

        public WallpaperSnapshot GetCurrent(string monitorId)
        {
            bool hasCalendar = GeneratedFileMarker.IsGenerated(_currentPath);
            return new WallpaperSnapshot(_currentPath, hasCalendar ? _currentPath : null, hasCalendar);
        }

        public void SetWallpaper(string monitorId, string imageFilePath)
        {
            SetWallpaperCalled++;
            LastSetPath = imageFilePath;
            _currentPath = imageFilePath; // после set «текущий фон» — наш файл
        }

        public string? GetOriginalPath(string monitorId) => null;

        public WallpaperFit ParseCurrentFit(string monitorId) => WallpaperFit.Fill;

        public void SetFit(string monitorId, WallpaperFit fit) => LastSetFit = fit;
    }

    private sealed class FakeMonitors : IMonitorService
    {
        private readonly MonitorInfo _info;
        public FakeMonitors(MonitorInfo info) => _info = info;
        public IReadOnlyList<MonitorInfo> GetMonitors() => new[] { _info };
        public MonitorInfo? GetById(string id) => id == _info.Id ? _info : null;
    }

    private sealed class FakePaths : IPlatformPaths
    {
        public FakePaths(string generated, string originals) { GeneratedDir = generated; OriginalsDir = originals; }
        public string AppDataDir { get; } = "/tmp/dcc-fake";
        public string OriginalsDir { get; }
        public string GeneratedDir { get; }
        public string SettingsFile { get; } = "/tmp/dcc-fake/settings.json";
        public string LogFile { get; } = "/tmp/dcc-fake/app.log";
    }
}
