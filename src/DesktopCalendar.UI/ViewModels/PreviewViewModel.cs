using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using DesktopCalendar.Core.Wallpaper;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace DesktopCalendar.UI.ViewModels;

/// <summary>
/// Превью календаря: реагирует на изменения SettingsViewModel, перерисовывает битмап
/// (debounced 150мс) на фоне реальных обоев выбранного монитора.
/// Общий синглтон, биндится в разделах «Расположение» и «Календарь».
/// </summary>
public sealed partial class PreviewViewModel : ObservableObject
{
    private readonly SettingsViewModel _settings;
    private readonly PreviewRenderer _renderer;
    private readonly ILogger<PreviewViewModel> _logger;

    private CancellationTokenSource? _cts;

    [ObservableProperty] private Bitmap? _bitmap;
    [ObservableProperty] private string _status = string.Empty;
    [ObservableProperty] private bool _isVisible = true;

    public PreviewViewModel(SettingsViewModel settings, PreviewRenderer renderer, ILogger<PreviewViewModel> logger)
    {
        _settings = settings;
        _renderer = renderer;
        _logger = logger;
        _settings.Changed += (_, _) => Schedule();
    }

    /// <summary>Первый рендер после открытия окна.</summary>
    public void Initialize() => Schedule();

    private void Schedule()
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(150, token);
                Render();
            }
            catch (OperationCanceledException) { /* expected */ }
        }, token);
    }

    private void Render()
    {
        try
        {
            using var sk = _renderer.RenderPreview(_settings.Raw, DateOnly.FromDateTime(DateTime.Now));
            if (sk is null)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    Bitmap = null;
                    Status = "Сначала выберите монитор.";
                });
                return;
            }
            using var data = sk.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = new MemoryStream();
            data.SaveTo(stream);
            stream.Position = 0;
            var bmp = new Bitmap(stream);

            Dispatcher.UIThread.Post(() =>
            {
                Bitmap = bmp;
                Status = string.Empty;
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Preview render failed");
            Dispatcher.UIThread.Post(() =>
            {
                Status = $"Превью недоступно: {ex.Message}";
            });
        }
    }
}
