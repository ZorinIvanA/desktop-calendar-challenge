using CommunityToolkit.Mvvm.ComponentModel;
using DesktopCalendar.Core.Settings;
using DesktopCalendar.Core.Wallpaper;
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
    private readonly WallpaperApplier _applier;
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

    private CancellationTokenSource? _dailyRefreshCts;

    partial void OnSelectedSectionIndexChanged(int value) => OnPropertyChanged(nameof(ActiveSection));

    public MainViewModel(
        AppSettings settings,
        ISettingsStore store,
        WallpaperApplier applier,
        MonitorSectionViewModel monitorSection,
        LayoutSectionViewModel layoutSection,
        CalendarSectionViewModel calendarSection,
        GeneralSectionViewModel generalSection,
        PreviewViewModel preview,
        ILogger<MainViewModel> logger)
    {
        _settings = settings;
        _store = store;
        _applier = applier;
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
        StartDailyRefreshTimer();
    }

    /// <summary>
    /// Ежедневное автообновление (M7): раз в 30 мин проверяет смену дня. Если сменился
    /// И календарь сейчас установлен — перерисовывает. Покрывает сценарий «машина не
    /// перезагружалась, GUI открыто». Для полного покрытия (GUI закрыто) — cron/Task-Scheduler (docs).
    /// </summary>
    private void StartDailyRefreshTimer()
    {
        _dailyRefreshCts?.Cancel();
        _dailyRefreshCts = new CancellationTokenSource();
        var token = _dailyRefreshCts.Token;
        _ = Task.Run(async () =>
        {
            var lastDay = DateOnly.FromDateTime(DateTime.Now);
            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(30));
            while (await timer.WaitForNextTickAsync(token))
            {
                var today = DateOnly.FromDateTime(DateTime.Now);
                if (today == lastDay) continue;
                lastDay = today;
                try
                {
                    // Перерисовываем только если целевой монитор выбран.
                    if (!string.IsNullOrEmpty(_settings.TargetMonitorId))
                    {
                        _applier.Apply(_settings.TargetMonitorId, _settings, today);
                        _store.Save(_settings);
                        _logger.LogInformation("Daily refresh: re-applied calendar for {Date}.", today);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Daily refresh failed.");
                }
            }
        }, token);
    }

    /// <summary>Сохранить настройки на закрытии.</summary>
    public void Persist()
    {
        _dailyRefreshCts?.Cancel();
        _store.Save(_settings);
    }
}
