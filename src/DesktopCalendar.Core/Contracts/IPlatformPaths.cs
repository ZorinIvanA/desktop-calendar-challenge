namespace DesktopCalendar.Core.Contracts;

/// <summary>
/// Пути к каталогам и файлам приложения. Реализация общая (PlatformPaths),
/// но интерфейс в Core, чтобы платформенные сервисы могли его использовать.
/// </summary>
public interface IPlatformPaths
{
    /// <summary>%LOCALAPPDATA%\DesktopCalendar (Win) / ~/.local/share/DesktopCalendar (Linux).</summary>
    string AppDataDir { get; }

    /// <summary>Кэш исходных обоев по monitorId.</summary>
    string OriginalsDir { get; }

    /// <summary>Сгенерированные файлы с календарём.</summary>
    string GeneratedDir { get; }

    /// <summary>settings.json.</summary>
    string SettingsFile { get; }

    /// <summary>Файл лога.</summary>
    string LogFile { get; }
}
