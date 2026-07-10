using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Controls;
using static DesktopCalendar.UI.Views.ConverterUtil;

namespace DesktopCalendar.UI.Views;

/// <summary>
/// Раздел «Монитор». Code-behind минимален: позиции прямоугольников в Canvas задаются
/// в XAML через конвертеры нормализованных координат, а не вручную.
/// </summary>
public partial class MonitorSectionView : UserControl
{
    public MonitorSectionView()
    {
        InitializeComponent();
    }
}

/// <summary>
/// Конвертер выделения: true → оранжевая рамка (выбран), false → серая.
/// Используется для BorderBrush кнопок мониторов в схеме.
/// </summary>
public sealed class SelectionToBrushConverter : IValueConverter
{
    public static readonly SelectionToBrushConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
        {
            return b ? Brushes.Orange : Brushes.Gray;
        }
        return Brushes.Gray;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Конвертер нормализованной координаты (0..1) в пиксели внутри Canvas фиксированного размера.
/// Parameter — размер Canvas по соответствующей оси (строка или double из XAML).
/// Учитывает отступ pad, чтобы прямоугольники не прилипали к краю.
/// </summary>
public sealed class NormToPixelConverter : IValueConverter
{
    public static readonly NormToPixelConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double norm && TryGetDouble(parameter, out var size))
        {
            const double pad = 16;
            return pad + norm * (size - pad * 2);
        }
        return 0.0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Конвертер нормализованного размера (0..1) в пиксели с учётом отступа и минимума.</summary>
public sealed class NormSizeToPixelConverter : IValueConverter
{
    public static readonly NormSizeToPixelConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double norm && TryGetDouble(parameter, out var size))
        {
            const double pad = 16;
            return Math.Max(60, norm * (size - pad * 2));
        }
        return 60.0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

internal static class ConverterUtil
{
    public static bool TryGetDouble(object? v, out double result)
    {
        switch (v)
        {
            case double d: result = d; return true;
            case int i: result = i; return true;
            case string s when double.TryParse(s, CultureInfo.InvariantCulture, out var d2):
                result = d2; return true;
            default: result = 0; return false;
        }
    }
}
