using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesktopCalendar.Core.Settings;

namespace DesktopCalendar.UI.ViewModels;

/// <summary>
/// Раздел «Расположение»: 8 точек привязки + отступ от края.
/// </summary>
public partial class LayoutSectionViewModel : ObservableObject
{
    private readonly SettingsViewModel _settings;

    public LayoutSectionViewModel(SettingsViewModel settings, PreviewViewModel preview)
    {
        _settings = settings;
        Preview = preview;
        _selectedAnchor = settings.Anchor;
        _marginPx = settings.MarginPx;
    }

    /// <summary>Общий синглтон превью (биндится в UI разделов Layout/Calendar).</summary>
    public PreviewViewModel Preview { get; }

    /// <summary>Список всех 8 anchor для UI-схемы.</summary>
    public IReadOnlyList<Anchor> Anchors { get; } = new[]
    {
        Anchor.TopLeft, Anchor.TopCenter, Anchor.TopRight,
        Anchor.CenterLeft, Anchor.CenterRight,
        Anchor.BottomLeft, Anchor.BottomCenter, Anchor.BottomRight,
    };

    [ObservableProperty] private Anchor _selectedAnchor;
    partial void OnSelectedAnchorChanged(Anchor value) => _settings.Anchor = value;

    [ObservableProperty] private int _marginPx;
    partial void OnMarginPxChanged(int value) => _settings.MarginPx = value;

    /// <summary>Команда выбора anchor-точки кликом по схеме.</summary>
    [RelayCommand]
    private void SelectAnchor(Anchor anchor) => SelectedAnchor = anchor;
}
