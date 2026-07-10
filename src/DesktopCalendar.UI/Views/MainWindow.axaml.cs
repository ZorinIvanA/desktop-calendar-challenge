using Avalonia.Controls;
using DesktopCalendar.UI.ViewModels;

namespace DesktopCalendar.UI.Views;

/// <summary>
/// Главное окно. DataContext = MainViewModel (через DI). При открытии — инициализирует
/// разделы и превью (TopLevel уже создан, Avalonia Screens доступен).
/// При закрытии — сохраняет настройки.
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(MainViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        if (DataContext is MainViewModel vm)
        {
            vm.OnWindowOpened();
        }
    }

    /// <summary>При закрытии приложения сохраняем настройки и сбрасываем debounce на диск.</summary>
    protected override void OnClosed(EventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.Persist();
        }

        if (UI.App.Services.GetService(typeof(Core.Settings.DebouncedSettingsStore))
            is Core.Settings.DebouncedSettingsStore debounced)
        {
            debounced.Flush();
        }
        base.OnClosed(e);
    }
}
