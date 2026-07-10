using DesktopCalendar.Core.Contracts;
using DesktopCalendar.Core.Platform;
using Microsoft.Extensions.DependencyInjection;

namespace DesktopCalendar.Platform.Linux;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Регистрирует Linux-реализации: IMonitorIdentityProvider (GNOME Mutter D-Bus),
    /// IWallpaperService (M4), IAutorunService (M6).
    /// IMonitorService (композитор) и IGeometryProvider (Avalonia) регистрируются в Core/UI.
    /// </summary>
    public static IServiceCollection AddLinux(this IServiceCollection services)
    {
        services.AddSingleton<MutterDisplayConfig>();
        services.AddSingleton<GnomeGsettings>();
        services.AddSingleton<IMonitorIdentityProvider, LinuxMonitorIdentityProvider>();
        services.AddSingleton<IWallpaperService, LinuxWallpaperService>();
        services.AddSingleton<IAutorunService, LinuxAutorunService>();
        return services;
    }
}
