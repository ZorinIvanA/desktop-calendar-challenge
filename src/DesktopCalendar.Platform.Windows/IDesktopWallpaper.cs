#if !WINDOWS_LITE
using System.Runtime.InteropServices;

namespace DesktopCalendar.Platform.Windows;

/// <summary>
/// CLSID coclass DesktopWallpaper (shobjidl_core.h). Вынесен в отдельный класс, а не в
/// COM-интерфейс: статическое поле внутри [ComImport]-интерфейса — нетипично и на некоторых
/// конфигурациях вызывает BadImageFormatException в ..cctor при первой загрузке типа.
/// </summary>
public static class DesktopWallpaperClsid
{
    public static readonly Guid Value = new("C2CF3110-460E-4fc1-B9D0-8A1C0C9CC4BD");
}

/// <summary>
/// COM-интерфейс IDesktopWallpaper (shobjidl_core.h).
/// IID   {B92B56A9-8B55-4E14-9A89-0199BBB6F93B}.
///
/// Vtable-порядок методов строго по shobjidl_core.idl:
/// https://learn.microsoft.com/en-us/windows/win32/api/shobjidl_core/nn-shobjidl_core-idesktopwallpaper
/// ВАЖНО: GetMonitorDevicePathAt ИДЁТ ДО GetMonitorDevicePathCount.
/// Интерфейс содержит ТОЛЬКО методы (никаких полей — иначе CLR падает на ..cctor).
/// </summary>
[ComImport]
[Guid("B92B56A9-8B55-4E14-9A89-0199BBB6F93B")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IDesktopWallpaper
{
    void SetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string monitorID, [MarshalAs(UnmanagedType.LPWStr)] string wallpaper);

    [return: MarshalAs(UnmanagedType.LPWStr)]
    string GetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string monitorID);

    // ВНИМАНИЕ: At перед Count (исправление бага M2).
    [return: MarshalAs(UnmanagedType.LPWStr)]
    string GetMonitorDevicePathAt(uint monitorIndex);

    uint GetMonitorDevicePathCount();

    Rect GetMonitorRECT([MarshalAs(UnmanagedType.LPWStr)] string monitorID);

    void SetPosition(DesktopWallpaperPosition position);
    DesktopWallpaperPosition GetPosition();
}

/// <summary>DESKTOP_WALLPAPER_POSITION enum (shobjidl_core.h).</summary>
public enum DesktopWallpaperPosition
{
    Center = 0,
    Tile = 1,
    Stretch = 2,
    Fit = 3,
    Fill = 4,
    Span = 5,
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
#endif
