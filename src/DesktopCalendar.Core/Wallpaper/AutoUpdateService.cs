using DesktopCalendar.Core.Contracts;
using DesktopCalendar.Core.Settings;
using Microsoft.Extensions.Logging;

namespace DesktopCalendar.Core.Wallpaper;

/// <summary>
/// Silent-режим: обновляет календарь на рабочем столе выбранного монитора без UI.
/// Запускается с ключом -auto (приложение / системный планировщик / авторан при логине).
///
/// Коды возврата:
///   0 — успех (или идемпотентный no-op: сегодня уже применено).
///   2 — не выбран целевой монитор.
///   3 — выбранный монитор недоступен (отключён).
///   1 — ошибка.
/// </summary>
public sealed class AutoUpdateService
{
    /// <summary>Код возврата: целевой монитор не выбран.</summary>
    public const int ExitNoMonitor = 2;
    /// <summary>Код возврата: монитор недоступен.</summary>
    public const int ExitMonitorGone = 3;
    /// <summary>Код возврата: ошибка.</summary>
    public const int ExitError = 1;
    /// <summary>Код возврата: успех.</summary>
    public const int ExitOk = 0;

    private readonly ISettingsStore _store;
    private readonly IWallpaperService _wallpaper;
    private readonly IMonitorService _monitors;
    private readonly WallpaperApplier _applier;
    private readonly ILogger<AutoUpdateService> _logger;

    public AutoUpdateService(
        ISettingsStore store,
        IWallpaperService wallpaper,
        IMonitorService monitors,
        WallpaperApplier applier,
        ILogger<AutoUpdateService> logger)
    {
        _store = store;
        _wallpaper = wallpaper;
        _monitors = monitors;
        _applier = applier;
        _logger = logger;
    }

    /// <summary>Выполнить тихое обновление. Возвращает код выхода для Main.</summary>
    public int Run()
    {
        var settings = _store.Load();

        if (string.IsNullOrEmpty(settings.TargetMonitorId))
        {
            _logger.LogWarning("Auto: target monitor not set, exiting {Code}.", ExitNoMonitor);
            return ExitNoMonitor;
        }

        // В silent-режиме окно не создаётся → IGeometryProvider (Avalonia Screens) недоступен.
        // Разрешение берём из настроек (сохраняется при выборе монитора в UI).
        // Если разрешения нет — монитор не выбран в UI или настройки повреждены.
        if (!settings.LastMonitorWidth.HasValue || !settings.LastMonitorHeight.HasValue)
        {
            _logger.LogWarning("Auto: monitor {Monitor} resolution unknown (not selected in UI), exiting {Code}.",
                settings.TargetMonitorId, ExitMonitorGone);
            return ExitMonitorGone;
        }

        // Идемпотентность: если сегодня (UTC) уже применяли И текущие обои == нашему файлу — выходим без работы.
        if (IsAlreadyAppliedToday(settings, settings.TargetMonitorId))
        {
            _logger.LogInformation("Auto: already applied today, no-op. Exiting {Code}.", ExitOk);
            return ExitOk;
        }

        try
        {
            // today для рендера — локальное (календарь меняется в локальную полночь).
            var today = DateOnly.FromDateTime(DateTime.Now);
            _applier.Apply(settings.TargetMonitorId, settings, today);
            _store.Save(settings);
            _logger.LogInformation("Auto: calendar applied to {Monitor}. Exiting {Code}.", settings.TargetMonitorId, ExitOk);
            return ExitOk;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Auto: apply failed. Exiting {Code}.", ExitError);
            return ExitError;
        }
    }

    /// <summary>
    /// Идемпотентность: LastAppliedUtc сегодня (UTC) И текущие обои = нашему последнему файлу.
    /// Настройки хранят UTC, сравниваем в UTC. Если пользователь сменил обои — currentUri != LastGeneratedPath → перерисуем.
    /// </summary>
    private bool IsAlreadyAppliedToday(AppSettings settings, string monitorId)
    {
        if (!settings.LastAppliedUtc.HasValue) return false;
        if (settings.LastAppliedUtc.Value.Date != DateTime.UtcNow.Date) return false;

        try
        {
            var snapshot = _wallpaper.GetCurrent(monitorId);
            return snapshot.HasCalendar
                   && !string.IsNullOrEmpty(settings.LastGeneratedPath)
                   && string.Equals(snapshot.CurrentUri, settings.LastGeneratedPath, StringComparison.Ordinal);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Auto: failed to query wallpaper state, proceeding with re-apply.");
            return false;
        }
    }
}
