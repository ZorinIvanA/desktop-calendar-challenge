using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DesktopCalendar.UI.Views;
using Microsoft.Extensions.DependencyInjection;

namespace DesktopCalendar.UI;

/// <summary>
/// Корень Avalonia-приложения. MainWindow строится из DI: MainWindow и MainViewModel
/// регистрируются в AddUi() и резолвятся здесь.
/// </summary>
public class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = Services.GetRequiredService<MainWindow>();
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>Вызывается из точки входа после построения DI-контейнера.</summary>
    public static void ConfigureServices(IServiceProvider services) => Services = services;
}
