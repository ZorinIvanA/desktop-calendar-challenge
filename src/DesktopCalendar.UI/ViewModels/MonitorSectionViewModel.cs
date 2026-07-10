using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesktopCalendar.Core.Contracts;
using Microsoft.Extensions.Logging;

namespace DesktopCalendar.UI.ViewModels;

/// <summary>
/// Раздел «Монитор»: список мониторов с визуальной схемой, выбор целевого.
/// В M5 пишет выбор через SettingsViewModel (чтобы превью и HasCalendar обновлялись реактивно).
/// </summary>
public partial class MonitorSectionViewModel : ObservableObject
{
    private readonly IMonitorService _monitorService;
    private readonly SettingsViewModel _settings;
    private readonly ILogger<MonitorSectionViewModel> _logger;

    public ObservableCollection<MonitorViewModel> Monitors { get; } = new();

    [ObservableProperty] private MonitorViewModel? _selectedMonitor;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isGnomeWarningVisible;

    public MonitorSectionViewModel(
        IMonitorService monitorService,
        SettingsViewModel settings,
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
        private void SelectMonitor(MonitorViewModel? monitorVm)
        {
            if (monitorVm is null) return;
            // monitorVm — это UI-обёртка; найдём исходный MonitorInfo для разрешения.
            var info = _monitorService.GetById(monitorVm.Id);
            if (info is null) return;

            foreach (var m in Monitors) m.IsSelected = false;
            monitorVm.IsSelected = true;
            SelectedMonitor = monitorVm;
            // Пишем через SettingsViewModel — это поднимет Changed и обновит превью + HasCalendar.
            _settings.TargetMonitorId = info.Id;
            // Сохраняем разрешение — нужно silent-режиму (без UI/Avalonia) для рендера.
            _settings.Raw.LastMonitorWidth = info.ResolutionWidth;
            _settings.Raw.LastMonitorHeight = info.ResolutionHeight;
            _settings.Raw.LastMonitorScaling = info.Scaling;
        }
}
