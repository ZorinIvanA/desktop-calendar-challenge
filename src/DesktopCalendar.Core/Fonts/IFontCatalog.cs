namespace DesktopCalendar.Core.Fonts;

/// <summary>
/// Доступ к каталогу системных шрифтов. Реализация через SKFontManager (SkiaSharp).
/// В UI — источник для выпадающего списка выбора шрифта.
/// </summary>
public interface IFontCatalog
{
    /// <summary>Все семейства шрифтов, доступные в системе (отсортированные).</summary>
    IReadOnlyList<string> GetFontFamilies();

    /// <summary>True, если запрошенное семейство реально установлено в системе.</summary>
    bool IsAvailable(string fontFamily);
}
