using DesktopCalendar.Core.Contracts;

namespace DesktopCalendar.Platform.Windows;

#if !WINDOWS_LITE
using System.Runtime.Versioning;
using Microsoft.Win32;

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
        key.SetValue(ValueName, $"\"{executablePath}\" {arguments}");
    }

    public void Disable()
    {
        if (!OperatingSystem.IsWindows()) return;
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        key?.DeleteValue(ValueName, throwOnMissingValue: false);
    }
}
#else
/// <summary>Заглушка для сборки на Linux (WINDOWS_LITE).</summary>
public sealed class WindowsAutorunService : IAutorunService
{
    public bool IsEnabled() => false;
    public void Enable(string executablePath, string arguments)
        => throw new PlatformNotSupportedException("Windows registry доступен только при сборке под Windows.");
    public void Disable()
        => throw new PlatformNotSupportedException();
}
#endif
