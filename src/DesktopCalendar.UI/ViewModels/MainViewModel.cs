using CommunityToolkit.Mvvm.ComponentModel;
using DesktopCalendar.Core.Settings;
using Microsoft.Extensions.Logging;

namespace DesktopCalendar.UI.ViewModels;

/// <summary>
/// Корневая ViewModel: навигация по разделам, общий SettingsViewModel.
/// Превью вынесено в PreviewViewModel (общий синглтон для разделов Layout/Calendar).
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly ISettingsStore _store;
    private readonly AppSettings _settings;
    private readonly ILogger<MainViewModel> _logger;

    public SettingsViewModel Settings { get; }
    public MonitorSectionViewModel MonitorSection { get; }
    public LayoutSectionViewModel LayoutSection { get; }
    public CalendarSectionViewModel CalendarSection { get; }
    public GeneralSectionViewModel GeneralSection { get; }
    public PreviewViewModel Preview { get; }

    public IReadOnlyList<string> Sections { get; } = new[] { "Монитор", "Расположение", "Календарь", "Общие" };

    [ObservableProperty] private int _selectedSectionIndex = 0;

    /// <summary>Активная section-ViewModel для ContentControl (DataTemplate по типу).</summary>
    public object ActiveSection => SelectedSectionIndex switch
    {
        0 => MonitorSection,
        1 => LayoutSection,
        2 => CalendarSection,
        3 => GeneralSection,
        _ => MonitorSection,
    };

    partial void OnSelectedSectionIndexChanged(int value) => OnPropertyChanged(nameof(ActiveSection));

    public MainViewModel(
        AppSettings settings,
        ISettingsStore store,
        MonitorSectionViewModel monitorSection,
        LayoutSectionViewModel layoutSection,
        CalendarSectionViewModel calendarSection,
        GeneralSectionViewModel generalSection,
        PreviewViewModel preview,
        ILogger<MainViewModel> logger)
    {
        _settings = settings;
        _store = store;
        _logger = logger;

        Settings = new SettingsViewModel(settings);
        MonitorSection = monitorSection;
        LayoutSection = layoutSection;
        CalendarSection = calendarSection;
        GeneralSection = generalSection;
        Preview = preview;
    }

    /// <summary>Вызывается из MainWindow.OnOpened: окно создано, можно инициализировать разделы.</summary>
    public void OnWindowOpened()
    {
        MonitorSection.LoadMonitors();
        GeneralSection.Initialize();
        Preview.Initialize();
        Settings.Changed += (_, _) => GeneralSection.RefreshHasCalendar();
    }

    /// <summary>Сохранить настройки на закрытии.</summary>
    public void Persist()
    {
        _store.Save(_settings);
    }
}
