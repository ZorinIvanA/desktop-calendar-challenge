using System.Runtime.InteropServices;

namespace DesktopCalendar.Platform.Windows;

/// <summary>
/// COM-интерфейс IDesktopWallpaper (shobjidl_core).
/// CLSID {C2CF3110-460E-4fc1-B9D0-8A1C0C9CC4BD}, IID {B92B56A9-8B55-4E14-9A89-0199BBB6F93B}.
/// В M2 объявлены только методы перечисления мониторов.
/// GetWallpaper/SetWallpaper/GetPosition/SetPosition добавятся в M4.
/// </summary>
[ComImport]
[Guid("B92B56A9-8B55-4E14-9A89-0199BBB6F93B")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IDesktopWallpaper
{
    /// <summary>CLSID DesktopWallpaper coclass для CreateInstance.</summary>
    internal static readonly Guid Clsid = new("C2CF3110-460E-4fc1-B9D0-8A1C0C9CC4BD");

    void SetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string monitorID, [MarshalAs(UnmanagedType.LPWStr)] string wallpaper);
    [return: MarshalAs(UnmanagedType.LPWStr)] string GetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string monitorID);

    /// <summary>Количество мониторов с назначенными обоями (включая отключённые).</summary>
    uint GetMonitorDevicePathCount();

    /// <summary>Device path монитора по индексу (стабильный EDID-based идентификатор).</summary>
    [return: MarshalAs(UnmanagedType.LPWStr)] string GetMonitorDevicePathAt(uint monitorIndex);

    /// <summary>Геометрия монитора в виртуальных координатах. S_FALSE для отключённого.</summary>
    Rect GetMonitorRECT([MarshalAs(UnmanagedType.LPWStr)] string monitorID);

    // Далее сигнатуры для M4 — приведены для корректного vtable, но не вызываются в M2.
    uint GetBackgroundColor();
    void SetBackgroundColor(uint color);
    uint GetPosition();
    void SetPosition(uint position);
}

/// <summary>Win32 RECT из GetMonitorRECT (виртуальные координаты, physical pixels).</summary>
[StructLayout(LayoutKind.Sequential)]
public struct Rect
{
    public int left;
    public int top;
    public int right;
    public int bottom;
}
