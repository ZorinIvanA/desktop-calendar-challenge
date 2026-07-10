using DesktopCalendar.Core.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace DesktopCalendar.Platform.Windows;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Регистрирует Windows-реализации IMonitorService / IWallpaperService / IAutorunService.
    /// Вызывать только при OperatingSystem.IsWindows().
    /// </summary>
    public static IServiceCollection AddWindows(this IServiceCollection services)
    {
        services.AddSingleton<IMonitorService, WindowsMonitorService>();
        services.AddSingleton<IWallpaperService, WindowsWallpaperService>();
        services.AddSingleton<IAutorunService, WindowsAutorunService>();
        return services;
    }
}
