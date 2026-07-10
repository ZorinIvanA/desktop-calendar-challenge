using System.Diagnostics;
using DesktopCalendar.Core.Platform;

namespace DesktopCalendar.Platform.Linux;

/// <summary>
/// Доступ к org.gnome.Mutter.DisplayConfig.GetCurrentState через gdbus call.
/// Возвращает список мониторов со стабильными id (vendor:product:serial / connector) и bounds.
///
/// Формат GetCurrentState: (serial, layout, [(logical-monitor-struct), ...], properties).
/// logical-monitor-struct = (bounds-struct, scale, transform, primary, [(monitor-struct), ...]).
/// monitor-struct = (connector, vendor, product, serial, [(mode-struct), ...], properties).
/// Полная спецификация: org.gnome.Mutter.DisplayConfig.xml.
/// </summary>
public sealed class MutterDisplayConfig
{
    /// <summary>Запросить текущее состояние мониторов у Mutter через gdbus.</summary>
    public IReadOnlyList<MonitorIdentity> GetMonitors()
    {
        var stdout = RunGdbusGetCurrentState();
        if (string.IsNullOrWhiteSpace(stdout))
        {
            return Array.Empty<MonitorIdentity>();
        }

        return GVariantParser.ParseLogicalMonitors(stdout);
    }

    private static string RunGdbusGetCurrentState()
    {
        var psi = new ProcessStartInfo
        {
            FileName = "gdbus",
            Arguments = "call --session --dest org.gnome.Mutter.DisplayConfig " +
                        "--object-path /org/gnome/Mutter/DisplayConfig " +
                        "--method org.gnome.Mutter.DisplayConfig.GetCurrentState",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using var proc = Process.Start(psi);
        if (proc is null)
        {
            return string.Empty;
        }
        // 3 секунды достаточно; при зависании сессии не блокируем UI бесконечно.
        if (!proc.WaitForExit(3000))
        {
            try { proc.Kill(); } catch { /* ignore */ }
            return string.Empty;
        }
        return proc.StandardOutput.ReadToEnd();
    }
}
