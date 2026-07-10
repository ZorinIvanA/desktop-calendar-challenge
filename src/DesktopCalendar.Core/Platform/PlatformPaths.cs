using DesktopCalendar.Core.Contracts;

namespace DesktopCalendar.Core.Platform;

/// <summary>
/// Реализация IPlatformPaths через Environment.GetFolderPath(SpecialFolder.LocalApplicationData).
/// Win: %LOCALAPPDATA%\DesktopCalendar. Linux: ~/.local/share/DesktopCalendar.
/// </summary>
public sealed class PlatformPaths : IPlatformPaths
{
    private const string AppFolderName = "DesktopCalendar";

    public string AppDataDir { get; }
    public string OriginalsDir { get; }
    public string GeneratedDir { get; }
    public string SettingsFile { get; }
    public string LogFile { get; }

    public PlatformPaths()
    {
        var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        AppDataDir = Path.Combine(baseDir, AppFolderName);
        OriginalsDir = Path.Combine(AppDataDir, "originals");
        GeneratedDir = Path.Combine(AppDataDir, "generated");
        SettingsFile = Path.Combine(AppDataDir, "settings.json");
        LogFile = Path.Combine(AppDataDir, "app.log");
    }

    /// <summary>Создать все служебные каталоги, если их ещё нет.</summary>
    public void EnsureDirectories()
    {
        Directory.CreateDirectory(AppDataDir);
        Directory.CreateDirectory(OriginalsDir);
        Directory.CreateDirectory(GeneratedDir);
    }
}
