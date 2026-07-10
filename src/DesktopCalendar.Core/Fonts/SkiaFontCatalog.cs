using SkiaSharp;

namespace DesktopCalendar.Core.Fonts;

/// <summary>
/// Реализация IFontCatalog через SKFontManager.Default. На Linux читает fontconfig,
/// на Windows — установленные шрифты. Синглтон: создание SKFontManager дорого.
/// </summary>
public sealed class SkiaFontCatalog : IFontCatalog
{
    private readonly Lazy<IReadOnlyList<string>> _families;

    public SkiaFontCatalog()
    {
        _families = new Lazy<IReadOnlyList<string>>(Load);
    }

    public IReadOnlyList<string> GetFontFamilies() => _families.Value;

    public bool IsAvailable(string fontFamily)
        => _families.Value.Contains(fontFamily, StringComparer.Ordinal);

    private static IReadOnlyList<string> Load()
    {
        using var fm = SKFontManager.Default;
        return fm.FontFamilies
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
