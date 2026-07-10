namespace DesktopCalendar.Core.Platform;

/// <summary>
/// Источник геометрии экранов, не зависящий от ОС.
/// Реализация в UI-проекте через Avalonia TopLevel.Screens.All.
/// </summary>
public interface IGeometryProvider
{
    /// <summary>Все экраны системы с их геометрией и scaling.</summary>
    IReadOnlyList<ScreenGeometry> GetScreens();
}
