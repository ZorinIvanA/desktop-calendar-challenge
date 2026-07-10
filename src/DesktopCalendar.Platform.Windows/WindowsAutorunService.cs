using System.Runtime.Versioning;
using DesktopCalendar.Core.Contracts;
using Microsoft.Win32;

namespace DesktopCalendar.Platform.Windows;

/// <summary>
/// Windows-реализация IAutorunService через HKCU\Software\Microsoft\Windows\CurrentVersion\Run.
/// Per-user, без прав администратора. Значение = "&lt;exe&gt; -auto".
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsAutorunService : IAutorunService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "DesktopCalendar";

    public bool IsEnabled()
    {
        if (!OperatingSystem.IsWindows()) return false;
        return Registry.GetValue($"HKEY_CURRENT_USER\\{RunKeyPath}", ValueName, null) is not null;
    }

    public void Enable(string executablePath, string arguments)
    {
        if (!OperatingSystem.IsWindows()) return;
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
        // Путь в кавычках на случай пробелов, затем аргументы.
        key.SetValue(ValueName, $"\"{executablePath}\" {arguments}");
    }

    public void Disable()
    {
        if (!OperatingSystem.IsWindows()) return;
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        key?.DeleteValue(ValueName, throwOnMissingValue: false);
    }
}
