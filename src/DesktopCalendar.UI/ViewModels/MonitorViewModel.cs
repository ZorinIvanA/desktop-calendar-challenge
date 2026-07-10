using CommunityToolkit.Mvvm.ComponentModel;
using DesktopCalendar.Core.Contracts;

namespace DesktopCalendar.UI.ViewModels;

/// <summary>
/// Один монитор в визуальной схеме раздела «Монитор».
/// Нормализованные координаты (0..1) для позиционирования в Canvas схемы.
/// </summary>
public partial class MonitorViewModel : ObservableObject
{
    public string Id { get; }
    public int LogicalIndex { get; }
    public string FriendlyName { get; }
    public string ResolutionLabel { get; }
    public bool IsPrimary { get; }

    /// <summary>Нормализованная позиция и размер в виртуальном десктопе (0..1).</summary>
    public double NormX { get; }
    public double NormY { get; }
    public double NormWidth { get; }
    public double NormHeight { get; }

    [ObservableProperty] private bool _isSelected;

    public MonitorViewModel(MonitorInfo info, MonitorLayout layout)
    {
        Id = info.Id;
        LogicalIndex = info.LogicalIndex;
        FriendlyName = info.FriendlyName;
        ResolutionLabel = $"{info.ResolutionWidth}×{info.ResolutionHeight}";
        IsPrimary = info.IsPrimary;

        // Нормализация: сдвигаем всё в положительные координаты и масштабируем к размеру десктопа.
        NormX = (info.BoundsX - layout.OriginX) / layout.VirtualWidth;
        NormY = (info.BoundsY - layout.OriginY) / layout.VirtualHeight;
        NormWidth = info.BoundsWidth / layout.VirtualWidth;
        NormHeight = info.BoundsHeight / layout.VirtualHeight;
    }
}

/// <summary>Габариты виртуального десктопа для нормализации координат мониторов в схеме.</summary>
public sealed record MonitorLayout(double OriginX, double OriginY, double VirtualWidth, double VirtualHeight)
{
    public static MonitorLayout From(IEnumerable<MonitorInfo> monitors)
    {
        var list = monitors as IList<MonitorInfo> ?? monitors.ToList();
        if (list.Count == 0)
        {
            return new MonitorLayout(0, 0, 1, 1);
        }
        var minX = list.Min(m => m.BoundsX);
        var minY = list.Min(m => m.BoundsY);
        var maxX = list.Max(m => m.BoundsX + m.BoundsWidth);
        var maxY = list.Max(m => m.BoundsY + m.BoundsHeight);
        return new MonitorLayout(minX, minY, maxX - minX, maxY - minY);
    }
}
