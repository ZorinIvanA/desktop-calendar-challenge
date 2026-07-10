using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesktopCalendar.Core.Contracts;
using DesktopCalendar.Core.Wallpaper;
using Microsoft.Extensions.Logging;

namespace DesktopCalendar.UI.ViewModels;

/// <summary>
/// Раздел «Общие»: чекбокс автозапуска (дизейблен, M6) + кнопки «Установить/Убрать календарь»
/// с реальным вызовом WallpaperApplier и динамическим переключением по HasCalendar.
/// </summary>
public partial class GeneralSectionViewModel : ObservableObject
{
    private readonly SettingsViewModel _settings;
    private readonly WallpaperApplier _applier;
    private readonly IWallpaperService _wallpaper;
    private readonly ILogger<GeneralSectionViewModel> _logger;

    public GeneralSectionViewModel(
        SettingsViewModel settings,
        WallpaperApplier applier,
        IWallpaperService wallpaper,
        ILogger<GeneralSectionViewModel> logger)
    {
        _settings = settings;
        _applier = applier;
        _wallpaper = wallpaper;
        _logger = logger;
    }

    /// <summary>Инициализация после создания окна. Запрашивает текущее состояние обоев.</summary>
    public void Initialize() => RefreshHasCalendar();

    [ObservableProperty] private bool _hasCalendar;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusMessage = string.Empty;

    /// <summary>Монитор выбран → можно применять.</summary>
    public bool CanApply => !string.IsNullOrEmpty(_settings.TargetMonitorId);
    public bool IsApplyVisible => !HasCalendar && !IsBusy && CanApply;
    public bool IsRemoveVisible => HasCalendar && !IsBusy && CanApply;

    /// <summary>Обновить HasCalendar по текущему состоянию рабочего стола выбранного монитора.</summary>
    public void RefreshHasCalendar()
    {
        var id = _settings.TargetMonitorId;
        if (string.IsNullOrEmpty(id))
        {
            HasCalendar = false;
            StatusMessage = "Сначала выберите монитор в разделе «Монитор».";
            return;
        }
        try
        {
            HasCalendar = _wallpaper.GetCurrent(id).HasCalendar;
            StatusMessage = HasCalendar
                ? "Календарь сейчас на рабочем столе."
                : "Календарь не установлен.";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to query wallpaper state");
            HasCalendar = false;
            StatusMessage = $"Не удалось определить состояние: {ex.Message}";
        }
        NotifyButtonVisibility();
    }

    private void NotifyButtonVisibility()
    {
        OnPropertyChanged(nameof(IsApplyVisible));
        OnPropertyChanged(nameof(IsRemoveVisible));
        OnPropertyChanged(nameof(CanApply));
    }

    [RelayCommand(CanExecute = nameof(CanApplyNow))]
    private async Task ApplyAsync()
    {
        var id = _settings.TargetMonitorId;
        if (string.IsNullOrEmpty(id)) return;

        IsBusy = true;
        StatusMessage = "Применяется…";
        NotifyButtonVisibility();
        try
        {
            await Task.Run(() => _applier.Apply(id, _settings.Raw, DateOnly.FromDateTime(DateTime.Now)));
            RefreshHasCalendar();
            StatusMessage = HasCalendar ? "Календарь установлен." : "Не удалось установить.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Apply failed");
            StatusMessage = $"Ошибка: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            NotifyButtonVisibility();
        }
    }

    [RelayCommand(CanExecute = nameof(CanRemoveNow))]
    private async Task RemoveAsync()
    {
        var id = _settings.TargetMonitorId;
        if (string.IsNullOrEmpty(id)) return;

        IsBusy = true;
        StatusMessage = "Убирается…";
        NotifyButtonVisibility();
        try
        {
            await Task.Run(() => _applier.Remove(id, _settings.Raw));
            RefreshHasCalendar();
            StatusMessage = !HasCalendar ? "Календарь убран, обои восстановлены." : "Не удалось убрать.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Remove failed");
            StatusMessage = $"Ошибка: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            NotifyButtonVisibility();
        }
    }

    private bool CanApplyNow() => CanApply && !IsBusy && !HasCalendar;
    private bool CanRemoveNow() => CanApply && !IsBusy && HasCalendar;
}
