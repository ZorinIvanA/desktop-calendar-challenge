using DesktopCalendar.Core.Calendar;
using DesktopCalendar.Core.Contracts;
using DesktopCalendar.Core.Settings;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace DesktopCalendar.Core.Wallpaper;

/// <summary>
/// OS-agnostic оркестратор применения/убирания календаря. Связывает воедино:
/// IWallpaperService (платформенный) + BackgroundFitter + CalendarRenderer + IPlatformPaths.
///
/// Сохранность оригинала (TZ §4.8): при первом apply копируем оригинал в OriginalsDir;
/// при смене обоев пользователем извне — автоматически подхватываем новый оригинал.
/// Cleanup: после apply удаляем все прежние сгенерированные файлы этого монитора.
/// </summary>
public class WallpaperApplier
{
    private readonly IWallpaperService _wallpaper;
    private readonly IMonitorService _monitors;
    private readonly IPlatformPaths _paths;
    private readonly ILogger<WallpaperApplier> _logger;

    public WallpaperApplier(
        IWallpaperService wallpaper,
        IMonitorService monitors,
        IPlatformPaths paths,
        ILogger<WallpaperApplier> logger)
    {
        _wallpaper = wallpaper;
        _monitors = monitors;
        _paths = paths;
        _logger = logger;
    }

    /// <summary>Применить календарь на рабочий стол выбранного монитора.</summary>
    public virtual void Apply(string monitorId, AppSettings settings, DateOnly today, double renderDpi = 96.0)
    {
        // Разрешение: из настроек (сохраняется при выборе в UI), иначе из IMonitorService.
        // Это позволяет silent-режиму работать без Avalonia (окно не создано → Screens недоступен).
        var monitor = _monitors.GetById(monitorId);
        int width = settings.LastMonitorWidth ?? monitor?.ResolutionWidth
                    ?? throw new InvalidOperationException(
                        $"Разрешение монитора '{monitorId}' неизвестно. Выберите монитор в UI.");
        int height = settings.LastMonitorHeight ?? monitor?.ResolutionHeight
                    ?? throw new InvalidOperationException(
                        $"Разрешение монитора '{monitorId}' неизвестно. Выберите монитор в UI.");

        var snapshot = _wallpaper.GetCurrent(monitorId);
        bool externalChange = !snapshot.HasCalendar
                              && snapshot.CurrentUri != settings.LastOriginalPath
                              && !string.IsNullOrEmpty(snapshot.CurrentUri);

        // 1. Сохранить/обновить оригинал, если нужно.
        string originalPath = EnsureOriginal(monitorId, snapshot, settings);
        settings.LastOriginalPath = originalPath;

        // 2. Запомнить исходный fit ОС (чтобы восстановить при remove), если ещё не запомнен.
        if (!settings.OriginalFit.HasValue || externalChange)
        {
            settings.OriginalFit = _wallpaper.ParseCurrentFit(monitorId);
        }

        // 3. Загрузить оригинал, привести к размеру монитора.
        using var originalBmp = LoadOriginalOrDefault(originalPath);
        WallpaperFit fit = settings.OriginalFit ?? WallpaperFit.Fill;
        using var monitorCanvas = BackgroundFitter.FitToMonitor(originalBmp, width, height, fit);

        // 4. Рендер календаря поверх.
        using var renderer = new CalendarRenderer(today.Year, today.Month, today);
        using var withCalendar = renderer.Render(monitorCanvas, settings, renderDpi);

        // 5. Сохранить результат.
        var now = DateTime.UtcNow;
        var fileName = GeneratedFileMarker.BuildFileName(monitorId, now);
        var outPath = Path.Combine(_paths.GeneratedDir, fileName);
        Directory.CreateDirectory(_paths.GeneratedDir);
        using (var data = withCalendar.Encode(SKEncodedImageFormat.Png, 100))
        {
            File.WriteAllBytes(outPath, data.ToArray());
        }

        // 6. Применить как обои в режиме 1:1 (Center) — канвас уже под разрешение монитора.
        _wallpaper.SetWallpaper(monitorId, outPath);
        _wallpaper.SetFit(monitorId, WallpaperFit.Center);

        // 7. Cleanup прежних сгенерированных файлов этого монитора.
        CleanupOldGenerated(monitorId, keep: outPath);

        // 8. Обновить служебные поля настроек.
        settings.LastGeneratedPath = outPath;
        settings.LastAppliedUtc = now;

        _logger.LogInformation("Calendar applied to monitor {Monitor}: {File}", monitorId, outPath);
    }

    /// <summary>Убрать календарь — восстановить оригинальные обои.</summary>
    public void Remove(string monitorId, AppSettings settings)
    {
        var original = settings.LastOriginalPath;
        if (string.IsNullOrEmpty(original) || !File.Exists(original))
        {
            _logger.LogWarning("Cannot remove calendar: no saved original for monitor {Monitor}", monitorId);
            return;
        }

        // Восстановить fit, который был до apply.
        if (settings.OriginalFit.HasValue)
        {
            _wallpaper.SetFit(monitorId, settings.OriginalFit.Value);
        }

        _wallpaper.SetWallpaper(monitorId, original);

        // Удалить сгенерированный файл (больше не актуален).
        if (settings.LastGeneratedPath is not null && File.Exists(settings.LastGeneratedPath))
        {
            try { File.Delete(settings.LastGeneratedPath); } catch { /* best effort */ }
        }
        settings.LastGeneratedPath = null;
        settings.LastAppliedUtc = null;

        _logger.LogInformation("Calendar removed from monitor {Monitor}, original restored: {Original}",
            monitorId, original);
    }

    /// <summary>
    /// Гарантировать наличие сохранённого оригинала в OriginalsDir.
    /// Если текущие обои — наш файл, оригинал уже сохранён (используем кэш).
    /// Если нет — копируем текущий файл пользователя.
    /// Если picture-uri = 'none' (нет файла) — создаём однотонный PNG как оригинал-заглушку.
    /// </summary>
    private string EnsureOriginal(string monitorId, WallpaperSnapshot snapshot, AppSettings settings)
    {
        if (snapshot.HasCalendar && !string.IsNullOrEmpty(settings.LastOriginalPath)
            && File.Exists(settings.LastOriginalPath))
        {
            return settings.LastOriginalPath; // уже сохраняли, обои сейчас наши
        }

        if (string.IsNullOrEmpty(snapshot.CurrentUri))
        {
            // 'none' — создадим однотонный оригинал, чтобы remove вернул что-то осмысленное.
            return CreateSolidOriginal(monitorId);
        }

        var sourcePath = snapshot.CurrentUri;
        var ext = Path.GetExtension(sourcePath);
        if (string.IsNullOrEmpty(ext)) ext = ".png";
        var dest = Path.Combine(_paths.OriginalsDir, Sanitize(monitorId) + ext);
        Directory.CreateDirectory(_paths.OriginalsDir);

        try
        {
            File.Copy(sourcePath, dest, overwrite: true);
            _logger.LogInformation("Saved original wallpaper: {Src} -> {Dest}", sourcePath, dest);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to copy original {Src}, creating solid fallback", sourcePath);
            return CreateSolidOriginal(monitorId);
        }
        return dest;
    }

    private string CreateSolidOriginal(string monitorId)
    {
        var dest = Path.Combine(_paths.OriginalsDir, Sanitize(monitorId) + "_solid.png");
        Directory.CreateDirectory(_paths.OriginalsDir);
        using var bmp = new SKBitmap(new SKImageInfo(16, 16, SKColorType.Bgra8888, SKAlphaType.Premul));
        using (var sk = new SKCanvas(bmp)) sk.Clear(new SKColor(0x2b, 0x58, 0x76));
        using var data = bmp.Encode(SKEncodedImageFormat.Png, 100);
        File.WriteAllBytes(dest, data.ToArray());
        return dest;
    }

    private static SKBitmap LoadOriginalOrDefault(string path)
    {
        if (File.Exists(path))
        {
            return SKBitmap.Decode(path) ?? CreateSolidBitmap();
        }
        return CreateSolidBitmap();
    }

    private static SKBitmap CreateSolidBitmap()
    {
        var bmp = new SKBitmap(new SKImageInfo(1920, 1080, SKColorType.Bgra8888, SKAlphaType.Premul));
        using var sk = new SKCanvas(bmp);
        sk.Clear(new SKColor(0x2b, 0x58, 0x76));
        return bmp;
    }

    private void CleanupOldGenerated(string monitorId, string keep)
    {
        var dir = _paths.GeneratedDir;
        if (!Directory.Exists(dir)) return;

        var prefix = Sanitize(monitorId) + GeneratedFileMarker.FileNameMarker;
        foreach (var f in Directory.EnumerateFiles(dir, prefix + "*"))
        {
            if (!string.Equals(f, keep, StringComparison.Ordinal))
            {
                try { File.Delete(f); } catch { /* best effort */ }
            }
        }
    }

    private static string Sanitize(string monitorId)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sb = new System.Text.StringBuilder();
        foreach (var c in monitorId)
        {
            sb.Append(Array.IndexOf(invalid, c) >= 0 ? '_' : c);
        }
        return sb.ToString();
    }
}
