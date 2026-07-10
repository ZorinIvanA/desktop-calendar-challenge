using CommunityToolkit.Mvvm.ComponentModel;
using DesktopCalendar.Core.Settings;

namespace DesktopCalendar.UI.ViewModels;

/// <summary>
/// Обёртка над AppSettings с двусторонней связью UI ↔ model.
/// Каждое поле expose'ится как ObservableProperty; в OnXChanged пишется обратно в AppSettings
/// и поднимается событие PreviewInvalidated для перерисовки превью.
/// </summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly AppSettings _settings;

    /// <summary>Поднимается при изменении любого поля, влияющего на превью/применение.</summary>
    public event EventHandler? Changed;

    public SettingsViewModel(AppSettings settings)
    {
        _settings = settings;
        // Инициализация ObservableProperty из AppSettings.
        _targetMonitorId = settings.TargetMonitorId;
        _anchor = settings.Anchor;
        _marginPx = settings.MarginPx;
        _fontFamily = settings.FontFamily;
        _fontSize = settings.FontSize;
        _colorMonth = settings.ColorMonth;
        _colorWeekday = settings.ColorWeekday;
        _colorDay = settings.ColorDay;
        _colorToday = settings.ColorToday;
        _colorWeekend = settings.ColorWeekend;
        _otherMonthMode = settings.OtherMonthMode;
        _otherMonthOpacity = settings.OtherMonthOpacity;
        _labelsPosition = settings.LabelsPosition;
        _autorun = settings.Autorun;
    }

    // --- Целевой монитор ---
    [ObservableProperty] private string? _targetMonitorId;
    partial void OnTargetMonitorIdChanged(string? value) { _settings.TargetMonitorId = value; RaiseChanged(); }

    // --- Расположение ---
    [ObservableProperty] private Anchor _anchor;
    partial void OnAnchorChanged(Anchor value) { _settings.Anchor = value; RaiseChanged(); }

    [ObservableProperty] private int _marginPx;
    partial void OnMarginPxChanged(int value) { _settings.MarginPx = value; RaiseChanged(); }

    // --- Шрифт ---
    [ObservableProperty] private string _fontFamily;
    partial void OnFontFamilyChanged(string value) { _settings.FontFamily = value; RaiseChanged(); }

    [ObservableProperty] private double _fontSize;
    partial void OnFontSizeChanged(double value) { _settings.FontSize = value; RaiseChanged(); }

    // --- Цвета ---
    [ObservableProperty] private PresetColor _colorMonth;
    partial void OnColorMonthChanged(PresetColor value) { _settings.ColorMonth = value; RaiseChanged(); }

    [ObservableProperty] private PresetColor _colorWeekday;
    partial void OnColorWeekdayChanged(PresetColor value) { _settings.ColorWeekday = value; RaiseChanged(); }

    [ObservableProperty] private PresetColor _colorDay;
    partial void OnColorDayChanged(PresetColor value) { _settings.ColorDay = value; RaiseChanged(); }

    [ObservableProperty] private PresetColor? _colorToday;
    partial void OnColorTodayChanged(PresetColor? value) { _settings.ColorToday = value; RaiseChanged(); }

    [ObservableProperty] private PresetColor? _colorWeekend;
    partial void OnColorWeekendChanged(PresetColor? value) { _settings.ColorWeekend = value; RaiseChanged(); }

    // --- Дни соседних месяцев ---
    [ObservableProperty] private OtherMonthMode _otherMonthMode;
    partial void OnOtherMonthModeChanged(OtherMonthMode value) { _settings.OtherMonthMode = value; RaiseChanged(); }

    [ObservableProperty] private byte _otherMonthOpacity;
    partial void OnOtherMonthOpacityChanged(byte value) { _settings.OtherMonthOpacity = value; RaiseChanged(); }

    // --- Подписи ---
    [ObservableProperty] private LabelsPosition _labelsPosition;
    partial void OnLabelsPositionChanged(LabelsPosition value) { _settings.LabelsPosition = value; RaiseChanged(); }

    // --- Общие ---
    [ObservableProperty] private bool _autorun;
    partial void OnAutorunChanged(bool value) { _settings.Autorun = value; RaiseChanged(); }

    private void RaiseChanged() => Changed?.Invoke(this, EventArgs.Empty);

    /// <summary>Прямой доступ к нижележащему AppSettings (для WallpaperApplier).</summary>
    public AppSettings Raw => _settings;
}
