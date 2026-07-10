using CommunityToolkit.Mvvm.ComponentModel;
using DesktopCalendar.Core.Contracts;

namespace DesktopCalendar.UI.ViewModels;

/// <summary>
/// Один монитор в визуальной схеме раздела «Монитор».
/// Хранит АБСОЛЮТНЫЕ координаты в Canvas-области схемы (CanvasWidth × CanvasHeight),
/// рассчитанные из виртуального десктопа с сохранением aspect ratio (fit-contain).
/// Прямоугольники мониторов пропорциональны и не искажаются/не пересекаются визуально.
/// </summary>
public partial class MonitorViewModel : ObservableObject
{
    public string Id { get; }
    public int LogicalIndex { get; }
    public string FriendlyName { get; }
    public string ResolutionLabel { get; }
    public bool IsPrimary { get; }

    /// <summary>Абсолютные позиция/размер в Canvas схемы (px), с учётом aspect ratio и отступа.</summary>
    public double CanvasX { get; }
    public double CanvasY { get; }
    public double CanvasWidth { get; }
    public double CanvasHeight { get; }

    [ObservableProperty] private bool _isSelected;

    public MonitorViewModel(MonitorInfo info, MonitorLayout layout)
    {
        Id = info.Id;
        LogicalIndex = info.LogicalIndex;
        FriendlyName = info.FriendlyName;
        ResolutionLabel = $"{info.ResolutionWidth}×{info.ResolutionHeight}";
        IsPrimary = info.IsPrimary;

        // Нормализованные (0..1) координаты в виртуальном десктопе.
        double nx = layout.VirtualWidth > 0 ? (info.BoundsX - layout.OriginX) / layout.VirtualWidth : 0;
        double ny = layout.VirtualHeight > 0 ? (info.BoundsY - layout.OriginY) / layout.VirtualHeight : 0;
        double nw = layout.VirtualWidth > 0 ? info.BoundsWidth / layout.VirtualWidth : 1;
        double nh = layout.VirtualHeight > 0 ? info.BoundsHeight / layout.VirtualHeight : 1;

        // Fit-contain виртуального десктопа в Canvas-область с preserve aspect ratio.
        var (cx, cy, cw, ch) = layout.ProjectToCanvas(nx, ny, nw, nh);
        CanvasX = cx;
        CanvasY = cy;
        CanvasWidth = cw;
        CanvasHeight = ch;
    }
}

/// <summary>
/// Габариты виртуального десктопа + проекция в Canvas-схему с сохранением aspect ratio.
/// fit-contain: виртуальный десктоп вписывается в CanvasWidth × CanvasHeight (по умолчанию
/// 660×360) с центрированием и отступом, чтобы прямоугольники мониторов были пропорциональны
/// и не пересекались визуально независимо от раскладки (горизонталь/вертикаль/со смещением).
/// </summary>
public sealed record MonitorLayout(double OriginX, double OriginY, double VirtualWidth, double VirtualHeight)
{
    // Фиксированный размер Canvas-области схемы (ширина, высота).
    public const double CanvasWidth = 660;
    public const double CanvasHeight = 360;
    private const double Pad = 16;

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

    /// <summary>
    /// Спроецировать нормализованный прямоугольник (0..1) в абсолютные координаты Canvas,
    /// сохраняя aspect ratio виртуального десктопа (fit-contain с центрированием + отступ).
    /// </summary>
    public (double x, double y, double w, double h) ProjectToCanvas(double nx, double ny, double nw, double nh)
    {
        double availW = CanvasWidth - Pad * 2;
        double availH = CanvasHeight - Pad * 2;

        double aspect = VirtualWidth > 0 && VirtualHeight > 0 ? VirtualWidth / VirtualHeight : 1.0;
        // Масштаб: какой выбрать, чтобы вписать aspect в availW×availH.
        double scale = aspect >= (availW / availH)
            ? availW / VirtualWidth        // упирается в ширину
            : availH / VirtualHeight;      // упирается в высоту
        // Размер отмасштабированного десктопа.
        double projW = (VirtualWidth > 0 ? VirtualWidth : 1) * scale;
        double projH = (VirtualHeight > 0 ? VirtualHeight : 1) * scale;
        // Центрируем в Canvas-области.
        double offX = Pad + (availW - projW) / 2;
        double offY = Pad + (availH - projH) / 2;

        return (offX + nx * projW, offY + ny * projH, nw * projW, nh * projH);
    }
}
