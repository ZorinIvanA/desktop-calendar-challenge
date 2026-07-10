namespace DesktopCalendar.Core.Contracts;

/// <summary>
/// Информация об одном мониторе в терминах ОС.
/// </summary>
/// <param name="Id">Стабильный идентификатор: device path (Windows) / connector name (Linux).</param>
/// <param name="LogicalIndex">1-based индекс в том порядке, как видит пользователь (1, 2, …).</param>
/// <param name="FriendlyName">Человекочитаемое имя для UI.</param>
/// <param name="BoundsX">X левого-верхнего угла в виртуальных координатах рабочего стола.</param>
/// <param name="BoundsY">Y левого-верхнего угла.</param>
/// <param name="BoundsWidth">Ширина рабочей области в пикселях.</param>
/// <param name="BoundsHeight">Высота рабочей области в пикселях.</param>
/// <param name="ResolutionWidth">Физическое разрешение по X.</param>
/// <param name="ResolutionHeight">Физическое разрешение по Y.</param>
public sealed record MonitorInfo(
    string Id,
    int LogicalIndex,
    string FriendlyName,
    int BoundsX,
    int BoundsY,
    int BoundsWidth,
    int BoundsHeight,
    int ResolutionWidth,
    int ResolutionHeight)
{
    public bool IsPrimary => BoundsX == 0 && BoundsY == 0;
}
