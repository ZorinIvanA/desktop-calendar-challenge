using Microsoft.Extensions.Logging;

namespace DesktopCalendar.Core.Settings;

/// <summary>
/// Декоратор над ISettingsStore: запись откладывается на <paramref name="delay"/>,
/// чтобы не писать файл на каждое изменение свойства в UI. Многократные вызовы Save
/// в пределах окна debounce'а схлопываются в одну запись последнего значения.
/// Чтение — напрямую из внутреннего store.
/// </summary>
public sealed class DebouncedSettingsStore : ISettingsStore, IDisposable
{
    private readonly ISettingsStore _inner;
    private readonly TimeSpan _delay;
    private readonly ILogger<DebouncedSettingsStore> _logger;

    private readonly object _gate = new();
    private AppSettings? _pending;
    private Timer? _timer;

    public DebouncedSettingsStore(ISettingsStore inner, TimeSpan delay, ILogger<DebouncedSettingsStore> logger)
    {
        _inner = inner;
        _delay = delay;
        _logger = logger;
    }

    public AppSettings Load() => _inner.Load();

    public void Save(AppSettings settings)
    {
        lock (_gate)
        {
            _pending = settings;
            _timer?.Dispose();
            _timer = new Timer(_ => Flush(), null, _delay, Timeout.InfiniteTimeSpan);
        }
    }

    /// <summary>Немедленно записать отложенное значение, если есть. Используется при закрытии приложения.</summary>
    public void Flush()
    {
        AppSettings? toWrite;
        lock (_gate)
        {
            toWrite = _pending;
            _pending = null;
            _timer?.Dispose();
            _timer = null;
        }

        if (toWrite is not null)
        {
            try
            {
                _inner.Save(toWrite);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Flush failed for debounced settings save.");
            }
        }
    }

    public void Dispose()
    {
        Flush();
    }
}
