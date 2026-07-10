using DesktopCalendar.Core.Contracts;
using DesktopCalendar.Core.Platform;
using DesktopCalendar.Core.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DesktopCalendar.Core;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Регистрирует OS-агностичное ядро: пути, настройки (JSON + debounce), MonitorService.
    /// IGeometryProvider регистрируется UI-проектом (AddUi), IMonitorIdentityProvider —
    /// платформенными проектами (AddWindows / AddLinux); оба опциональны для degraded-режима.
    /// </summary>
    public static IServiceCollection AddCore(this IServiceCollection services)
    {
        var paths = new PlatformPaths();
        paths.EnsureDirectories();
        services.AddSingleton<IPlatformPaths>(paths);

        // JsonSettingsStore — конкретная реализация, DebouncedSettingsStore — декоратор.
        services.AddSingleton<JsonSettingsStore>(sp =>
            new JsonSettingsStore(
                paths.SettingsFile,
                sp.GetRequiredService<ILogger<JsonSettingsStore>>()));

        services.AddSingleton<DebouncedSettingsStore>(sp =>
            new DebouncedSettingsStore(
                sp.GetRequiredService<JsonSettingsStore>(),
                TimeSpan.FromMilliseconds(500),
                sp.GetRequiredService<ILogger<DebouncedSettingsStore>>()));

        // ISettingsStore указывает на декоратор, чтобы UI получал debounce "из коробки".
        services.AddSingleton<ISettingsStore>(sp => sp.GetRequiredService<DebouncedSettingsStore>());

        // MonitorService собирается из IGeometryProvider (обязателен) и IMonitorIdentityProvider
        // (опционален: null → degraded-режим без стабильного id).
        services.AddSingleton<IMonitorService>(sp =>
            new MonitorService(
                sp.GetRequiredService<IGeometryProvider>(),
                sp.GetService<IMonitorIdentityProvider>()));

        // WallpaperApplier — оркестратор apply/remove (M4). IWallpaperService/IMonitorService
        // регистрируются платформенными проектами.
        services.AddSingleton<Wallpaper.WallpaperApplier>();

        // M5: каталог шрифтов и рендерер превью.
        services.AddSingleton<Fonts.IFontCatalog, Fonts.SkiaFontCatalog>();
        services.AddSingleton<Wallpaper.PreviewRenderer>();

        // M6: silent-режим (AutoUpdateService) + идемпотентность.
        services.AddSingleton<Wallpaper.AutoUpdateService>();

        return services;
    }
}

