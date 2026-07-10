using System.IO;

namespace DesktopCalendar.Core.Wallpaper;

/// <summary>
/// Соглашение об именовании сгенерированных файлов и детект "наш ли это файл" по пути.
/// Файлы WallpaperApplier пишет как &lt;monitorId&gt;_&lt;yyyy-MM-dd&gt;_&lt;HHmmss&gt;.png внутри GeneratedDir.
/// Сервисы обоев идентифицируют "наш календарь стоит сейчас?" по совпадению этого шаблона.
/// </summary>
public static class GeneratedFileMarker
{
    /// <summary>Префикс/паттерн: имя файла содержит "_cal_" как маркер, что это наш вывод.</summary>
    public const string FileNameMarker = "_cal_";

    /// <summary>Расширение сгенерированного файла.</summary>
    public const string Extension = ".png";

    /// <summary>Построить имя файла для генерации. Milliseconds добавлены для уникальности
    /// при многократном apply в течение одной секунды (M6 идемпотентность + cleanup).</summary>
    public static string BuildFileName(string monitorId, DateTime utcTimestamp) =>
        $"{Sanitize(monitorId)}{FileNameMarker}{utcTimestamp:yyyy-MM-dd_HHmmss_fff}{Extension}";

    /// <summary>True, если путь указывает на наш сгенерированный файл (по маркеру в имени).</summary>
    public static bool IsGenerated(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        var name = Path.GetFileName(path);
        return name.Contains(FileNameMarker, StringComparison.Ordinal)
               && name.EndsWith(Extension, StringComparison.OrdinalIgnoreCase);
    }

    private static string Sanitize(string monitorId)
    {
        // monitorId на Windows содержит символы путей (\\?\DISPLAY#...); сделаем безопасное имя.
        var invalid = Path.GetInvalidFileNameChars();
        var sb = new System.Text.StringBuilder();
        foreach (var c in monitorId)
        {
            sb.Append(Array.IndexOf(invalid, c) >= 0 ? '_' : c);
        }
        return sb.ToString();
    }
}
