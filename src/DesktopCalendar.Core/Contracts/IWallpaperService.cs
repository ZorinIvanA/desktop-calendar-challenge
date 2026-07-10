namespace DesktopCalendar.Core.Contracts;

/// <summary>
/// Доступ к обоям рабочего стола. Реализация платформенная.
/// На GNOME per-monitor поддерживается ограниченно (см. TZ §2.2, SPECIFICATION §4.2).
/// </summary>
public interface IWallpaperService
{
    /// <summary>Что сейчас стоит на мониторе, и наш это файл или оригинал.</summary>
    WallpaperSnapshot GetCurrent(string monitorId);

    /// <summary>Установить файл изображения как обои монитора.</summary>
    void SetWallpaper(string monitorId, string imageFilePath);

    /// <summary>Путь к сохранённому оригиналу обоев монитора (или null, если ещё не сохраняли).</summary>
    string? GetOriginalPath(string monitorId);

    /// <summary>Текущий режим растяжения ОС для монитора.</summary>
    WallpaperFit ParseCurrentFit(string monitorId);
}
