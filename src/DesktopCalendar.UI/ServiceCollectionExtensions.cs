using DesktopCalendar.Core.Platform;
using DesktopCalendar.Core.Settings;
using DesktopCalendar.UI.Platform;
using DesktopCalendar.UI.ViewModels;
using DesktopCalendar.UI.Views;
using Microsoft.Extensions.DependencyInjection;

namespace DesktopCalendar.UI;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Регистрирует геометрию экранов (Avalonia), ViewModels и Views.
    /// </summary>
    public static IServiceCollection AddUi(this IServiceCollection services)
    {
        services.AddSingleton<IGeometryProvider, AvaloniaScreenGeometryProvider>();

        // AppSettings — синглтон на сессию: читаем один раз при старте, в M5 редактируем в UI.
        services.AddSingleton(sp => sp.GetRequiredService<ISettingsStore>().Load());
        services.AddSingleton<MonitorSectionViewModel>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainWindow>();
        return services;
    }
}
