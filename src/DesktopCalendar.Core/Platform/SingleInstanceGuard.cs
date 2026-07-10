using System.Threading;

namespace DesktopCalendar.Core.Platform;

/// <summary>
/// Гарантирует, что запущен только один экземпляр GUI-приложения.
/// Windows: именованный Mutex (глобальный, пер-user через prefix).
/// Linux/прочие: lock-файл в AppDataDir с эксклюзивным FileStream (освобождается ОС
/// при смерти процесса — stale-файл не блокирует повторный запуск).
/// </summary>
public sealed class SingleInstanceGuard : IDisposable
{
    private readonly string _lockPath;
    private Mutex? _mutex;
    private FileStream? _fileLock;
    private bool _acquired;

    public SingleInstanceGuard(string appDataDir)
    {
        _lockPath = Path.Combine(appDataDir, ".single-instance.lock");
    }

    /// <summary>Попытаться стать единственным экземпляром. False — уже запущен другой.</summary>
    public bool TryAcquire()
    {
        if (OperatingSystem.IsWindows())
        {
            // Mutex per-user: имя с префиксом Local\ — видно в сессии пользователя.
            _mutex = new Mutex(initiallyOwned: true, name: @"Local\DesktopCalendar-SingleInstance",
                               out var createdNew);
            _acquired = createdNew;
            return _acquired;
        }

        // Linux/macOS: эксклюзивный FileStream на lock-файл.
        try
        {
            var dir = Path.GetDirectoryName(_lockPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            _fileLock = new FileStream(_lockPath, FileMode.Create, FileAccess.Write, FileShare.None);
            _acquired = true;
            return true;
        }
        catch
        {
            _acquired = false;
            return false;
        }
    }

    public void Dispose()
    {
        if (_acquired)
        {
            try { _fileLock?.Dispose(); } catch { /* ignore */ }
            try { if (_fileLock is not null && File.Exists(_lockPath)) File.Delete(_lockPath); } catch { /* ignore */ }
            try { _mutex?.ReleaseMutex(); } catch { /* ignore */ }
            _mutex?.Dispose();
            _acquired = false;
        }
    }
}
