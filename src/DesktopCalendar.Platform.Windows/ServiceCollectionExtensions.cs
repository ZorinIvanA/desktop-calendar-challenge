using DesktopCalendar.Core.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace DesktopCalendar.Platform.Windows;

#if !WINDOWS_LITE
using System.Runtime.Versioning;
#endif

public static class ServiceCollectionExtensions
{
#if !WINDOWS_LITE
    /// <summary>
    /// Регистрирует Windows-реализации: IMonitorIdentityProvider (IDesktopWallpaper),
    /// IWallpaperService, IAutorunService. Вызывать только при OperatingSystem.IsWindows().
    /// </summary>
    [SupportedOSPlatform("windows")]
#endif
    public static IServiceCollection AddWindows(this IServiceCollection services)
    {
        services.AddSingleton<Core.Platform.IMonitorIdentityProvider, WindowsMonitorIdentityProvider>();
        services.AddSingleton<IWallpaperService, WindowsWallpaperService>();
        services.AddSingleton<IAutorunService, WindowsAutorunService>();
        return services;
    }
}
