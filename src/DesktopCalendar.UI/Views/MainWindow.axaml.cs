using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using DesktopCalendar.UI.ViewModels;

namespace DesktopCalendar.UI.Views;

/// <summary>
/// Главное окно. DataContext = MainViewModel (через DI). При открытии — загружаем список
/// мониторов (TopLevel уже создан, Avalonia Screens доступен). При закрытии — сохраняем настройки.
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

        // Окно создано → TopLevel.Screens доступен → пора перечислить мониторы.
        if (DataContext is MainViewModel vm)
        {
            vm.MonitorSection.LoadMonitors();
        }
    }

    /// <summary>При закрытии приложения сохраняем настройки и сбрасываем debounce на диск.</summary>
    protected override void OnClosed(EventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.Persist();
        }

        // Persist() кладёт значение в debounced store — форсируем немедленную запись.
        if (UI.App.Services.GetService(typeof(Core.Settings.DebouncedSettingsStore))
            is Core.Settings.DebouncedSettingsStore debounced)
        {
            debounced.Flush();
        }
        base.OnClosed(e);
    }
}
