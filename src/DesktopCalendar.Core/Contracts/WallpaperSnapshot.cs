namespace DesktopCalendar.Core.Contracts;

/// <summary>
/// Снимок текущего состояния обоев конкретного монитора.
/// </summary>
/// <param name="CurrentUri">Что сейчас установлено (путь или file:// URI).</param>
/// <param name="LastGeneratedPath">Наш последний сгенерированный файл (если ставили календарь), иначе null.</param>
/// <param name="HasCalendar">Сейчас на мониторе стоит наш файл с календарём?</param>
public sealed record WallpaperSnapshot(
    string CurrentUri,
    string? LastGeneratedPath,
    bool HasCalendar);
