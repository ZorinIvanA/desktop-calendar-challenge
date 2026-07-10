using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DesktopCalendar.Core.Fonts;
using DesktopCalendar.Core.Settings;

namespace DesktopCalendar.UI.ViewModels;

/// <summary>
/// Раздел «Календарь»: шрифт/размер, цвета, опц. цвета today/weekend,
/// режим дней соседних месяцев, положение подписей.
/// </summary>
public partial class CalendarSectionViewModel : ObservableObject
{
    private readonly SettingsViewModel _settings;

    public CalendarSectionViewModel(SettingsViewModel settings, IFontCatalog fontCatalog, PreviewViewModel preview)
    {
        _settings = settings;
        Preview = preview;

        FontFamilies = new ObservableCollection<string>(fontCatalog.GetFontFamilies());
        if (!FontFamilies.Contains(settings.FontFamily))
        {
            // Сохранённого шрифта нет в системе — добавим в список, чтобы он отображался.
            FontFamilies.Insert(0, settings.FontFamily);
        }

        _selectedFontFamily = settings.FontFamily;
        _fontSize = settings.FontSize;
        _colorMonth = settings.ColorMonth;
        _colorWeekday = settings.ColorWeekday;
        _colorDay = settings.ColorDay;

        _highlightToday = settings.ColorToday.HasValue;
        _colorToday = settings.ColorToday ?? PresetColor.Red;
        _highlightWeekend = settings.ColorWeekend.HasValue;
        _colorWeekend = settings.ColorWeekend ?? PresetColor.Blue;

        _otherMonthMode = settings.OtherMonthMode;
        _otherMonthOpacity = settings.OtherMonthOpacity;
        _labelsPosition = settings.LabelsPosition;
    }

    public ObservableCollection<string> FontFamilies { get; }

    /// <summary>Общий синглтон превью.</summary>
    public PreviewViewModel Preview { get; }
    public IReadOnlyList<PresetColor> AvailableColors { get; } = new[]
    {
        PresetColor.White, PresetColor.Red, PresetColor.Green, PresetColor.Blue,
    };
    public IReadOnlyList<OtherMonthMode> OtherMonthModes { get; } = new[]
    {
        OtherMonthMode.Show, OtherMonthMode.Hide, OtherMonthMode.CustomOpacity,
    };
    public IReadOnlyList<LabelsPosition> LabelsPositions { get; } = new[]
    {
        LabelsPosition.Top, LabelsPosition.Bottom,
    };

    // --- Шрифт ---
    [ObservableProperty] private string _selectedFontFamily;
    partial void OnSelectedFontFamilyChanged(string value) => _settings.FontFamily = value;

    [ObservableProperty] private double _fontSize;
    partial void OnFontSizeChanged(double value) => _settings.FontSize = value;

    // --- Цвета ---
    [ObservableProperty] private PresetColor _colorMonth;
    partial void OnColorMonthChanged(PresetColor value) => _settings.ColorMonth = value;

    [ObservableProperty] private PresetColor _colorWeekday;
    partial void OnColorWeekdayChanged(PresetColor value) => _settings.ColorWeekday = value;

    [ObservableProperty] private PresetColor _colorDay;
    partial void OnColorDayChanged(PresetColor value) => _settings.ColorDay = value;

    // --- Опц. цвет сегодня ---
    [ObservableProperty] private bool _highlightToday;
    partial void OnHighlightTodayChanged(bool value)
        => _settings.ColorToday = value ? ColorToday : null;

    [ObservableProperty] private PresetColor _colorToday;
    partial void OnColorTodayChanged(PresetColor value)
    {
        if (_highlightToday) _settings.ColorToday = value;
    }

    // --- Опц. цвет выходных ---
    [ObservableProperty] private bool _highlightWeekend;
    partial void OnHighlightWeekendChanged(bool value)
        => _settings.ColorWeekend = value ? ColorWeekend : null;

    [ObservableProperty] private PresetColor _colorWeekend;
    partial void OnColorWeekendChanged(PresetColor value)
    {
        if (_highlightWeekend) _settings.ColorWeekend = value;
    }

    // --- Дни соседних месяцев ---
    [ObservableProperty] private OtherMonthMode _otherMonthMode;
    partial void OnOtherMonthModeChanged(OtherMonthMode value)
    {
        _settings.OtherMonthMode = value;
        OnPropertyChanged(nameof(IsOpacityVisible));
    }

    [ObservableProperty] private byte _otherMonthOpacity;
    partial void OnOtherMonthOpacityChanged(byte value) => _settings.OtherMonthOpacity = value;

    /// <summary>True если режим CustomOpacity выбран — показываем ползунок прозрачности.</summary>
    public bool IsOpacityVisible => OtherMonthMode == OtherMonthMode.CustomOpacity;

    // --- Подписи ---
    [ObservableProperty] private LabelsPosition _labelsPosition;
    partial void OnLabelsPositionChanged(LabelsPosition value) => _settings.LabelsPosition = value;
}
