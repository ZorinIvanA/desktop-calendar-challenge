using Avalonia.Controls;
using DesktopCalendar.UI.ViewModels;

namespace DesktopCalendar.UI.Views;

/// <summary>
/// Главное окно. DataContext ставится из DI (MainViewModel).
/// При закрытии — флашим debounced settings, чтобы настройки точно ушли на диск.
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

    /// <summary>При закрытии приложения сохраняем настройки и сбрасываем debounce на диск.</summary>
    protected override void OnClosed(EventArgs e)
    {
        if (DataContext is ViewModels.MainViewModel vm)
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
