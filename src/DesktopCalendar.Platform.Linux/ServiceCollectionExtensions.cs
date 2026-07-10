using DesktopCalendar.Core.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace DesktopCalendar.Platform.Linux;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Регистрирует Linux-реализации IMonitorService / IWallpaperService / IAutorunService.
    /// Вызывать только при OperatingSystem.IsLinux(). В M1 все реализации — заглушки;
    /// конкретный DE (GNOME и т.д.) определяется через DesktopEnvironment.Detect() в M4.
    /// </summary>
    public static IServiceCollection AddLinux(this IServiceCollection services)
    {
        services.AddSingleton<IMonitorService, LinuxMonitorService>();
        services.AddSingleton<IWallpaperService, LinuxWallpaperService>();
        services.AddSingleton<IAutorunService, LinuxAutorunService>();
        return services;
    }
}
