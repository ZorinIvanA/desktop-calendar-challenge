using DesktopCalendar.Core.Settings;
using SkiaSharp;

namespace DesktopCalendar.Core.Calendar;

/// <summary>
/// Палитра предустановленных цветов (SPECIFICATION §3). Единая точка правды для RGB-значений.
/// </summary>
public static class PresetColors
{
    public static SKColor ToSkColor(this PresetColor c) => c switch
    {
        PresetColor.White => new SKColor(0xFF, 0xFF, 0xFF),
        PresetColor.Red => new SKColor(0xE5, 0x39, 0x35),
        PresetColor.Green => new SKColor(0x43, 0xA0, 0x47),
        PresetColor.Blue => new SKColor(0x1E, 0x88, 0xE5),
        _ => SKColors.White,
    };

    /// <summary>Применить opacity (0..100) к цвету.</summary>
    public static SKColor WithOpacityPercent(this SKColor c, byte opacityPercent)
    {
        byte alpha = (byte)Math.Clamp((int)Math.Round(opacityPercent * 255.0 / 100.0), 0, 255);
        return c.WithAlpha(alpha);
    }
}
