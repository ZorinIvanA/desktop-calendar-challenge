using System.Runtime.Versioning;
using DesktopCalendar.Core.Contracts;
using DesktopCalendar.Core.Platform;
using Microsoft.Extensions.DependencyInjection;

namespace DesktopCalendar.Platform.Windows;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Регистрирует Windows-реализации: IMonitorIdentityProvider (IDesktopWallpaper),
    /// IWallpaperService, IAutorunService. Вызывать только при OperatingSystem.IsWindows().
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static IServiceCollection AddWindows(this IServiceCollection services)
    {
        services.AddSingleton<IMonitorIdentityProvider, WindowsMonitorIdentityProvider>();
        services.AddSingleton<IWallpaperService, WindowsWallpaperService>();
        services.AddSingleton<IAutorunService, WindowsAutorunService>();
        return services;
    }
}
