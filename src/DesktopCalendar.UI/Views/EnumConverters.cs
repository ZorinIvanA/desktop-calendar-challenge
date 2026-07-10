using System.Globalization;
using Avalonia.Data.Converters;
using DesktopCalendar.Core.Settings;

namespace DesktopCalendar.UI.Views;

/// <summary>Конвертеры enum ↔ bool для RadioButton-групп. Mode=TwoWay.</summary>
public sealed class OtherMonthModeToBoolConverter : IValueConverter
{
    public static readonly OtherMonthModeToBoolConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is OtherMonthMode m && parameter is string s && Enum.TryParse<OtherMonthMode>(s, out var p) && m == p;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && b && parameter is string s && Enum.TryParse<OtherMonthMode>(s, out var p) ? p : null;
}

public sealed class LabelsPositionToBoolConverter : IValueConverter
{
    public static readonly LabelsPositionToBoolConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is LabelsPosition l && parameter is string s && Enum.TryParse<LabelsPosition>(s, out var p) && l == p;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && b && parameter is string s && Enum.TryParse<LabelsPosition>(s, out var p) ? p : null;
}
