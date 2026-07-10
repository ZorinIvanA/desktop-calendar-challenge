using DesktopCalendar.Core.Contracts;

namespace DesktopCalendar.Platform.Linux;

/// <summary>
/// Linux/GNOME-реализация IAutorunService через XDG autostart: ~/.config/autostart/desktopcalendar.desktop.
/// GNOME (включая Wayland) читает этот каталог при старте сессии.
/// Disable удаляет файл → IsEnabled == File.Exists остаётся консистентным.
/// </summary>
public sealed class LinuxAutorunService : IAutorunService
{
    private const string FileName = "desktopcalendar.desktop";

    public bool IsEnabled() => File.Exists(GetDesktopFilePath());

    public void Enable(string executablePath, string arguments)
    {
        var path = GetDesktopFilePath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var content = $"""
            [Desktop Entry]
            Type=Application
            Name=Desktop Calendar
            Comment=Desktop Calendar silent updater
            Exec={EscapeExec(executablePath)} {arguments}
            TryExec={EscapeExec(executablePath)}
            Terminal=false
            X-GNOME-Autostart-enabled=true
            Hidden=false

            """;
        File.WriteAllText(path, content);
    }

    public void Disable()
    {
        var path = GetDesktopFilePath();
        if (File.Exists(path)) File.Delete(path);
    }

    private static string GetDesktopFilePath()
    {
        // SpecialFolder.ApplicationData на Linux = ~/.config (или $XDG_CONFIG_HOME).
        // SpecialFolderOption.Create гарантирует существование каталога.
        var configDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData,
                            Environment.SpecialFolderOption.Create);
        if (string.IsNullOrEmpty(configDir)) configDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
        return Path.Combine(configDir, "autostart", FileName);
    }

    /// <summary>Escape пути для поля Exec по desktop-entry spec: пробелы → \s, и т.п.</summary>
    private static string EscapeExec(string path)
    {
        if (string.IsNullOrEmpty(path)) return path;
        // Минимальный escape: пробел. Полный список зарезервированных символов редок в путях к exe.
        return path.Replace(" ", "\\s");
    }
}
