using DesktopCalendar.Core.Platform;

namespace DesktopCalendar.Platform.Linux;

/// <summary>
/// Linux-источник стабильных идентификаторов мониторов.
/// GNOME: через org.gnome.Mutter.DisplayConfig D-Bus (gdbus call) → connector + EDID vendor/product/serial.
/// Id = vendor:product:serial (fallback на connector, если serial пустой — дешёвые мониторы без EDID).
/// Прочие DE — PlatformNotSupportedException (out of scope по ТЗ).
/// </summary>
public sealed class LinuxMonitorIdentityProvider : IMonitorIdentityProvider
{
    private readonly MutterDisplayConfig _mutter;

    public LinuxMonitorIdentityProvider(MutterDisplayConfig mutter)
    {
        _mutter = mutter;
    }

    public IReadOnlyList<MonitorIdentity> GetIdentities()
    {
        var de = DesktopEnvironment.Detect();
        if (de != LinuxDesktop.Gnome)
        {
            throw new PlatformNotSupportedException(
                $"Linux DE '{DesktopEnvironment.DisplayName(de)}' не поддерживается. " +
                "Поддерживается только GNOME. См. TZ §2.1, §10.");
        }

        return _mutter.GetMonitors();
    }
}
