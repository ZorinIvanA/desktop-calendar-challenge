using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesktopCalendar.Core.Contracts;
using DesktopCalendar.Core.Settings;
using Microsoft.Extensions.Logging;

namespace DesktopCalendar.UI.ViewModels;

/// <summary>
/// Раздел «Монитор»: список мониторов с визуальной схемой, выбор целевого.
/// В M2 — единственный рабочий раздел; остальные добавятся в M5.
/// </summary>
public partial class MonitorSectionViewModel : ObservableObject
{
    private readonly IMonitorService _monitorService;
    private readonly AppSettings _settings;
    private readonly ILogger<MonitorSectionViewModel> _logger;

    public ObservableCollection<MonitorViewModel> Monitors { get; } = new();

    [ObservableProperty] private MonitorViewModel? _selectedMonitor;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isGnomeWarningVisible;

    public MonitorSectionViewModel(
        IMonitorService monitorService,
        AppSettings settings,
        ILogger<MonitorSectionViewModel> logger)
    {
        _monitorService = monitorService;
        _settings = settings;
        _logger = logger;
    }

    /// <summary>Загрузить список мониторов. Вызывается при активации раздела (окно уже создано).</summary>
    public void LoadMonitors()
    {
        Monitors.Clear();
        try
        {
            var monitors = _monitorService.GetMonitors();
            var layout = MonitorLayout.From(monitors);
            foreach (var m in monitors)
            {
                Monitors.Add(new MonitorViewModel(m, layout));
            }

            // Подсветить текущий выбранный.
            var currentId = _settings.TargetMonitorId;
            SelectedMonitor = string.IsNullOrEmpty(currentId)
                ? null
                : Monitors.FirstOrDefault(vm => vm.Id == currentId);

            StatusMessage = monitors.Count > 0
                ? $"Найдено мониторов: {monitors.Count}"
                : "Мониторы не найдены.";

            IsGnomeWarningVisible = OperatingSystem.IsLinux();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enumerate monitors.");
            StatusMessage = $"Ошибка получения списка мониторов: {ex.Message}";
            IsGnomeWarningVisible = false;
        }
    }

    /// <summary>Команда выбора монитора пользователем (клик по прямоугольнику в схеме).</summary>
    [RelayCommand]
    private void SelectMonitor(MonitorViewModel? monitor)
    {
        if (monitor is null) return;

        foreach (var m in Monitors) m.IsSelected = false;
        monitor.IsSelected = true;
        SelectedMonitor = monitor;
        _settings.TargetMonitorId = monitor.Id;

        // Сохранение debounce'ится в ISettingsStore; Persist() при закрытии окна флашит.
        // Здесь только помечаем settings «грязным» — фактический save через магазин.
        // (MainViewModel.Persist вызывается на OnClosed окна.)
    }
}
