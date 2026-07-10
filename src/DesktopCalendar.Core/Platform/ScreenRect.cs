namespace DesktopCalendar.Core.Platform;

/// <summary>
/// Прямоугольник в пикселях виртуального рабочего стола.
/// Собственный тип вместо Avalonia.PixelRect, чтобы Core не зависел от UI-фреймворка.
/// Координаты — в виртуальном десктопе: первичный монитор имеет левый-верхний угол (0,0),
/// вторичные могут иметь отрицательные X/Y.
/// </summary>
public readonly record struct ScreenRect(int X, int Y, int Width, int Height)
{
    public int Right => X + Width;
    public int Bottom => Y + Height;

    /// <summary>Площадь прямоугольника (для выбора максимального пересечения при сшивке).</summary>
    public int Area => Width * Height;

    /// <summary>Пересечение с другим прямоугольником; null, если не пересекаются.</summary>
    public ScreenRect? Intersect(ScreenRect other)
    {
        var x = Math.Max(X, other.X);
        var y = Math.Max(Y, other.Y);
        var right = Math.Min(Right, other.Right);
        var bottom = Math.Min(Bottom, other.Bottom);
        if (right <= x || bottom <= y)
        {
            return null;
        }
        return new ScreenRect(x, y, right - x, bottom - y);
    }

    public override string ToString() => $"({X},{Y}) {Width}x{Height}";
}
