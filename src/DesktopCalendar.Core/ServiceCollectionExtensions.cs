using DesktopCalendar.Core.Contracts;
using DesktopCalendar.Core.Platform;
using DesktopCalendar.Core.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DesktopCalendar.Core;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Регистрирует OS-агностичное ядро: пути, настройки (JSON + debounce).
    /// IMonitorService / IWallpaperService / IAutorunService регистрируются
    /// платформенными проектами (AddWindows / AddLinux).
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

        return services;
    }
}
