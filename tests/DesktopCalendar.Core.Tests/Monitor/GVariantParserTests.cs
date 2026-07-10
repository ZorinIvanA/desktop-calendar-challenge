using DesktopCalendar.Core.Platform;

namespace DesktopCalendar.Core.Tests.Monitor;

/// <summary>
/// Тесты парсера вывода gdbus org.gnome.Mutter.DisplayConfig.GetCurrentState.
/// Используют захардкоженные образцы (включая реальный вывод с машины разработчика),
/// чтобы парсер не зависел от живой D-Bus сессии.
/// </summary>
public sealed class GVariantParserTests
{
    /// <summary>Реальный вывод GetCurrentState с одного монитора eDP-1 (без EDID-serial → fallback).</summary>
    private const string SingleMonitorReal =
        "(uint32 1, [('1920x1080@60.001', 1920, 1080, 60.001, 1.0, [1.0], {'is-current': <true>})], " +
        "[(0, 0, 1.0, uint32 0, true, [('eDP-1', 'CMN', '0x1529', '0x00000000')], @a{sv} {})], " +
        "{'renderer': <'native'>})";

    /// <summary>Синтетика: два монитора, один с реальным serial, второй без.</summary>
    private const string TwoMonitors =
        "(uint32 2, [], " +
        "[(0, 0, 1.0, uint32 0, true, [('DP-1', 'GSM', '0xabc', '0x12345678')], {}), " +
        " (2560, 0, 1.5, uint32 0, false, [('HDMI-1', 'SAM', '0xdef', '0x00000000')], {})], " +
        "{})";

    private const string Empty =
        "(uint32 0, [], [], {})";

    [Fact]
    public void Parses_SingleMonitor_RealOutput()
    {
        var monitors = InvokeParse(SingleMonitorReal);

        monitors.Should().ContainSingle();
        var m = monitors[0];
        m.Connector.Should().Be("eDP-1");
        m.Id.Should().Be("eDP-1", "serial 0x00000000 → fallback на connector");
        m.LogicalIndex.Should().Be(1);
        m.Bounds.X.Should().Be(0);
        m.Bounds.Y.Should().Be(0);
    }

    [Fact]
    public void Parses_TwoMonitors_OneWithSerialOneFallback()
    {
        var monitors = InvokeParse(TwoMonitors);

        monitors.Should().HaveCount(2);
        // Первый — с реальным serial → vendor:product:serial.
        monitors[0].Connector.Should().Be("DP-1");
        monitors[0].Id.Should().Be("GSM:0xabc:0x12345678");
        monitors[0].LogicalIndex.Should().Be(1);
        monitors[0].Bounds.X.Should().Be(0);

        // Второй — без serial → fallback на connector.
        monitors[1].Connector.Should().Be("HDMI-1");
        monitors[1].Id.Should().Be("HDMI-1");
        monitors[1].LogicalIndex.Should().Be(2);
        monitors[1].Bounds.X.Should().Be(2560);
    }

    [Fact]
    public void Parses_EmptyMonitorsArray()
    {
        var monitors = InvokeParse(Empty);
        monitors.Should().BeEmpty();
    }

    [Fact]
    public void Parses_MalformedInput_ReturnsEmpty()
    {
        var monitors = InvokeParse("это не GVariant");
        monitors.Should().BeEmpty();
    }

    /// <summary>Парсер объявлен internal в Linux-проекте; тесты живут в Core.Tests.
    /// Для простоты — вызываем через рефлексию, чтобы не плодить отдельный тест-проект под Linux.</summary>
    private static IReadOnlyList<MonitorIdentity> InvokeParse(string gdbusOutput)
    {
        var asm = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "DesktopCalendar.Platform.Linux");
        if (asm is null)
        {
            throw new InvalidOperationException(
                "DesktopCalendar.Platform.Linux не загружен. Добавьте ProjectReference в тест-проект.");
        }
        var type = asm.GetType("DesktopCalendar.Platform.Linux.GVariantParser")
                   ?? throw new InvalidOperationException("Тип GVariantParser не найден.");
        var method = type.GetMethod("ParseLogicalMonitors",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
                     ?? throw new InvalidOperationException("Метод ParseLogicalMonitors не найден.");
        return (IReadOnlyList<MonitorIdentity>)method.Invoke(null, new object[] { gdbusOutput })!;
    }
}
