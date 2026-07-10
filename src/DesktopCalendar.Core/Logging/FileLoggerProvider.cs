using Microsoft.Extensions.Logging;

namespace DesktopCalendar.Core.Logging;

/// <summary>
/// Минимальный файловый логгер (без внешних зависимостей вроде Serilog).
/// Пишет в указанный файл, append, с timestamp и уровнем. Потокобезопасный через lock.
/// Используется в первую очередь в silent-режиме (-auto), где нет консоли.
/// </summary>
public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly string _logFile;
    private readonly object _gate = new();
    private readonly LogLevel _minLevel;

    public FileLoggerProvider(string logFile, LogLevel minLevel = LogLevel.Information)
    {
        _logFile = logFile;
        _minLevel = minLevel;
        try
        {
            var dir = Path.GetDirectoryName(logFile);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        }
        catch { /* best effort: если не смогли создать каталог, логи просто не запишутся */ }
    }

    public ILogger CreateLogger(string categoryName) => new FileLogger(_logFile, _gate, _minLevel, categoryName);

    public void Dispose() { }

    private sealed class FileLogger : ILogger
    {
        private readonly string _file;
        private readonly object _gate;
        private readonly LogLevel _minLevel;
        private readonly string _category;

        public FileLogger(string file, object gate, LogLevel minLevel, string category)
        {
            _file = file;
            _gate = gate;
            _minLevel = minLevel;
            _category = category;
        }

        public bool IsEnabled(LogLevel logLevel) => logLevel >= _minLevel;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            var msg = formatter(state, exception);
            var line = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} [{logLevel}] {_category}: {msg}";
            if (exception is not null) line += Environment.NewLine + exception;
            lock (_gate)
            {
                try { File.AppendAllText(_file, line + Environment.NewLine); }
                catch { /* swallow — лог не должен ронять приложение */ }
            }
        }
    }
}
