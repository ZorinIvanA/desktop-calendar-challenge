using DesktopCalendar.Core;
using DesktopCalendar.Platform.Linux;
using DesktopCalendar.Platform.Windows;
using DesktopCalendar.UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DesktopCalendar.App;

/// <summary>
/// Сборка DI-контейнера: регистрирует Core + UI всегда, платформенные сервисы — по текущей ОС.
/// </summary>
internal static class ServiceComposition
{
    public static IServiceProvider Build()
    {
        var services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Information);
            builder.AddSimpleConsole(opts =>
            {
                opts.SingleLine = true;
                opts.TimestampFormat = "HH:mm:ss ";
            });
        });

        services.AddCore();
        services.AddUi();

        if (OperatingSystem.IsWindows())
        {
            services.AddWindows();
        }
        else if (OperatingSystem.IsLinux())
        {
            services.AddLinux();
        }
        else
        {
            // macOS и прочее — вне scope ТЗ. Контракты не зарегистрированы;
            // любой их запрос упадёт при резолве, что приемлемо для неподдерживаемой ОС.
        }

        return services.BuildServiceProvider();
    }
}
