using DesktopCalendar.Core.Contracts;

namespace DesktopCalendar.Platform.Windows;

/// <summary>
/// Windows-реализация IAutorunService через HKCU\Software\Microsoft\Windows\CurrentVersion\Run.
/// Полная имплементация — в M6.
/// </summary>
public sealed class WindowsAutorunService : IAutorunService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "DesktopCalendar";

    public bool IsEnabled()
    {
        // TODO M6: Microsoft.Win32.Registry.GetValue($"HKEY_CURRENT_USER\{RunKeyPath}", ValueName, null).
        throw new NotImplementedException("WindowsAutorunService реализуется в M6.");
    }

    public void Enable(string executablePath, string arguments)
    {
        // TODO M6.
        throw new NotImplementedException("WindowsAutorunService реализуется в M6.");
    }

    public void Disable()
    {
        // TODO M6.
        throw new NotImplementedException("WindowsAutorunService реализуется в M6.");
    }
}
