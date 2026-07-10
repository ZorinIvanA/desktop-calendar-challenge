using DesktopCalendar.Core.Contracts;

namespace DesktopCalendar.Platform.Linux;

/// <summary>
/// Linux-реализация IAutorunService через ~/.config/autostart/*.desktop. В M1 — заглушка.
/// В M6: создаём desktop-файл с Exec=&lt;exe&gt; -auto, X-GNOME-Autostart-enabled=true.
/// </summary>
public sealed class LinuxAutorunService : IAutorunService
{
    public bool IsEnabled()
    {
        // TODO M6: проверить ~/.config/autostart/desktopcalendar.desktop.
        throw new NotImplementedException("LinuxAutorunService реализуется в M6.");
    }

    public void Enable(string executablePath, string arguments)
    {
        // TODO M6.
        throw new NotImplementedException("LinuxAutorunService реализуется в M6.");
    }

    public void Disable()
    {
        // TODO M6.
        throw new NotImplementedException("LinuxAutorunService реализуется в M6.");
    }
}
