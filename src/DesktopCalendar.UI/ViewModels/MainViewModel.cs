using CommunityToolkit.Mvvm.ComponentModel;
using DesktopCalendar.Core.Settings;

namespace DesktopCalendar.UI.ViewModels;

/// <summary>
/// M1: диагностическая ViewModel. Показывает, что settings-пайплайн работает:
/// значения читаются из ISettingsStore и биндятся к TextBlock.
/// Полноценная VM с командами и редактированием — в M5.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly ISettingsStore _store;
    private readonly AppSettings _settings;
    [ObservableProperty] private string _targetMonitorId;
    [ObservableProperty] private string _anchor;
    [ObservableProperty] private int _marginPx;
    [ObservableProperty] private string _fontFamily;
    [ObservableProperty] private double _fontSize;
    [ObservableProperty] private string _colorMonth;
    [ObservableProperty] private string _colorWeekday;
    [ObservableProperty] private string _colorDay;
    [ObservableProperty] private string _colorToday;
    [ObservableProperty] private string _colorWeekend;
    [ObservableProperty] private string _otherMonthMode;
    [ObservableProperty] private byte _otherMonthOpacity;
    [ObservableProperty] private string _labelsPosition;
    [ObservableProperty] private bool _autorun;

    public MainViewModel(AppSettings settings, ISettingsStore store)
    {
        _settings = settings;
        _store = store;

        _targetMonitorId = settings.TargetMonitorId ?? "(не выбран)";
        _anchor = settings.Anchor.ToString();
        _marginPx = settings.MarginPx;
        _fontFamily = settings.FontFamily;
        _fontSize = settings.FontSize;
        _colorMonth = settings.ColorMonth.ToString();
        _colorWeekday = settings.ColorWeekday.ToString();
        _colorDay = settings.ColorDay.ToString();
        _colorToday = settings.ColorToday?.ToString() ?? "—";
        _colorWeekend = settings.ColorWeekend?.ToString() ?? "—";
        _otherMonthMode = settings.OtherMonthMode.ToString();
        _otherMonthOpacity = settings.OtherMonthOpacity;
        _labelsPosition = settings.LabelsPosition.ToString();
        _autorun = settings.Autorun;
    }

    /// <summary>
    /// Сохранить текущие настройки через debounced store.
    /// В M1 вызывается при закрытии окна — чтобы settings.json появился и пережил перезапуск.
    /// </summary>
    public void Persist()
    {
        // В M1 настройки в UI не редактируются, поэтому пишем как есть — что прочитали.
        // В M5 здесь будет сборка AppSettings из отредактированных полей.
        _store.Save(_settings);
    }
}
