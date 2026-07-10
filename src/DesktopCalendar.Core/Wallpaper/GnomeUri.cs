namespace DesktopCalendar.Core.Wallpaper;

/// <summary>
/// Работа с GVariant-строками GNOME picture-uri.
///
/// gsettings get org.gnome.desktop.background picture-uri возвращает текстовое представление
/// GVariant-строки: одинарные кавычки + file:// URI внутри, напр.
///   'file:///usr/share/backgrounds/Northan_lights_by_mizuno.webp'
/// или 'none' (нет изображения, сплошной цвет).
///
/// Для gsettings set нужно передавать ТОТ ЖЕ формат: одинарные кавычки вокруг значения.
/// </summary>
public static class GnomeUri
{
    private const string NoneValue = "none";
    private const string FileScheme = "file://";

    /// <summary>
    /// Извлечь локальный путь из GVariant-значения picture-uri.
    /// Возвращает null для 'none' (нет файла). Бросает исключение при некорректном формате.
    /// </summary>
    public static string? PathFromPictureUri(string gvariantValue)
    {
        var s = gvariantValue.Trim();
        // Снимаем одинарные (или двойные) кавычки GVariant-строки.
        if (s.Length >= 2 && (s[0] == '\'' && s[^1] == '\'' || s[0] == '"' && s[^1] == '"'))
        {
            s = s[1..^1];
        }

        if (s == NoneValue) return null;

        if (!s.StartsWith(FileScheme, StringComparison.Ordinal))
        {
            // Уже путь или неизвестная схема — вернём как есть (мягкая деградация).
            return s;
        }

        // file:///abs/path → убираем scheme (оставляя /abs/path), декодируем %20 и т.п.
        var uriPart = s[FileScheme.Length..];
        // uriPart для абсолютного пути начинается с '/'. Декодируем percent-encoding.
        return Uri.UnescapeDataString(uriPart);
    }

    /// <summary>
    /// Построить GVariant-значение picture-uri из локального пути.
    /// Возвращает строку с одинарными кавычками и file:// префиксом,
    /// готовую к передаче в gsettings set.
    /// </summary>
    public static string ToGvariantUri(string localPath)
    {
        if (localPath == NoneValue)
        {
            return "'" + NoneValue + "'";
        }

        // Полный file:// URI с percent-encoding. UriBuilder строит корректный абсолютный URI.
        var uri = new UriBuilder("file", "", -1, localPath).Uri;
        // Для локального файла Uri.ToString() даёт "file:///abs/path" с кодированными пробелами.
        // Но LocalPath-style хотим как file:// + percent-encoded path.
        var abs = uri.AbsoluteUri; // file:///abs/path с %20
        return "'" + abs + "'";
    }

    /// <summary>Собрать GVariant-значение для 'none'.</summary>
    public static string NoneGvariant() => "'" + NoneValue + "'";
}
