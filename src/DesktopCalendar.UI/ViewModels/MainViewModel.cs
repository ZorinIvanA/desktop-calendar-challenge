using CommunityToolkit.Mvvm.ComponentModel;
using DesktopCalendar.Core.Settings;

namespace DesktopCalendar.UI.ViewModels;

/// <summary>
/// Корневая ViewModel: навигация по разделам и持有ание настроек.
/// В M2 активен только раздел «Монитор»; остальные добавятся в M5.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly ISettingsStore _store;
    private readonly AppSettings _settings;

    /// <summary>Раздел «Монитор».</summary>
    public MonitorSectionViewModel MonitorSection { get; }

    /// <summary>Доступные разделы для sidebar.</summary>
    public IReadOnlyList<string> Sections { get; } = new[] { "Монитор", "Расположение", "Календарь", "Общие" };

    [ObservableProperty] private int _selectedSectionIndex = 0;

    // Видимость разделов — производные от SelectedSectionIndex. XAML не умеет == в биндинге,
    // поэтому暴露им булевы свойства.
    public bool IsMonitorVisible => SelectedSectionIndex == 0;
    public bool IsLayoutVisible => SelectedSectionIndex == 1;
    public bool IsCalendarVisible => SelectedSectionIndex == 2;
    public bool IsGeneralVisible => SelectedSectionIndex == 3;

    public MainViewModel(AppSettings settings, ISettingsStore store, MonitorSectionViewModel monitorSection)
    {
        _settings = settings;
        _store = store;
        MonitorSection = monitorSection;
    }

    partial void OnSelectedSectionIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsMonitorVisible));
        OnPropertyChanged(nameof(IsLayoutVisible));
        OnPropertyChanged(nameof(IsCalendarVisible));
        OnPropertyChanged(nameof(IsGeneralVisible));
    }

    /// <summary>
    /// Сохранить текущие настройки через debounced store.
    /// </summary>
    public void Persist()
    {
        _store.Save(_settings);
    }
}
