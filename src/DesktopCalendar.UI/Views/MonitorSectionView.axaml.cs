using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Controls;

namespace DesktopCalendar.UI.Views;

/// <summary>
/// Раздел «Монитор». Code-behind минимален: позиции прямоугольников в Canvas заданы
/// абсолютными CanvasX/Y/Width/Height во MonitorViewModel (без конвертеров).
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
