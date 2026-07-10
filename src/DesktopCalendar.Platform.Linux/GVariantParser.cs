using System.Globalization;
using DesktopCalendar.Core.Platform;

namespace DesktopCalendar.Platform.Linux;

/// <summary>
/// Парсер текстового вывода gdbus call для org.gnome.Mutter.DisplayConfig.GetCurrentState.
///
/// Top-level: (serial, [layout-modes], [logical-monitors], {properties}).
/// logical-monitor: (x, y, scale, transform, primary, [(monitor,...)], {props}).
/// monitor: (connector, vendor, product, serial, [modes], {props}).
///
/// Подход: сканируем строку с учётом строковых литералов ('...') и balanced-скобок,
/// разбиваем кортежи/массивы на top-level элементы, достаём нужные скаляры.
/// Намеренно простой — полное GVariant-дерево не строим.
/// </summary>
internal static class GVariantParser
{
    public static IReadOnlyList<MonitorIdentity> ParseLogicalMonitors(string gdbusOutput)
    {
        var result = new List<MonitorIdentity>();
        if (string.IsNullOrWhiteSpace(gdbusOutput)) return result;

        var s = gdbusOutput.Trim();
        var topLevel = SplitTupleElements(s);
        // Ожидаем ≥3 элементов; logical-monitors — третий (index 2).
        if (topLevel.Count < 3) return result;

        var logicalsArray = topLevel[2];
        if (!logicalsArray.StartsWith('[')) return result;

        var logicalTuples = SplitArrayElements(logicalsArray);
        var index = 1;
        foreach (var tuple in logicalTuples)
        {
            var parsed = ParseLogicalMonitor(tuple, index);
            if (parsed is not null)
            {
                result.Add(parsed);
                index++;
            }
        }
        return result;
    }

    /// <summary>Разбить "(a, b, c)" на ["a","b","c"] по запятым на глубине 0, с учётом строк '...'.</summary>
    private static IReadOnlyList<string> SplitTupleElements(string tuple)
    {
        var t = tuple.Trim();
        // Снимаем только одну пару внешних скобок.
        if (t.StartsWith('(') && t.EndsWith(')')) t = t[1..^1];

        var parts = new List<string>();
        var acc = new System.Text.StringBuilder();
        int depth = 0;
        bool inString = false;

        for (int i = 0; i < t.Length; i++)
        {
            char c = t[i];
            if (inString)
            {
                acc.Append(c);
                if (c == '\'') inString = false;
                continue;
            }
            switch (c)
            {
                case '\'':
                    inString = true;
                    acc.Append(c);
                    break;
                case '(' or '[' or '<' or '{':
                    depth++;
                    acc.Append(c);
                    break;
                case ')' or ']' or '>' or '}':
                    depth--;
                    acc.Append(c);
                    break;
                case ',' when depth == 0:
                    parts.Add(acc.ToString().Trim());
                    acc.Clear();
                    break;
                default:
                    acc.Append(c);
                    break;
            }
        }
        if (acc.Length > 0 && !string.IsNullOrWhiteSpace(acc.ToString()))
            parts.Add(acc.ToString().Trim());
        return parts;
    }

    /// <summary>Разбить "[a, b, c]" на ["a","b","c"] (элементы — обычно кортежи "(...)").</summary>
    private static IReadOnlyList<string> SplitArrayElements(string array)
    {
        var a = array.Trim();
        if (a.StartsWith('[') && a.EndsWith(']')) a = a[1..^1];
        return SplitTupleElements("(" + a + ")"); // переиспользуем логику разделения
    }

    /// <summary>Парсит один logical-monitor: (x, y, scale, transform, primary, [monitors], props).</summary>
    private static MonitorIdentity? ParseLogicalMonitor(string tuple, int logicalIndex)
    {
        var parts = SplitTupleElements(tuple);
        if (parts.Count < 6) return null;

        if (!int.TryParse(parts[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var x)) return null;
        if (!int.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var y)) return null;
        if (!double.TryParse(parts[2].Trim(), NumberStyles.Float | NumberStyles.AllowDecimalPoint,
                             CultureInfo.InvariantCulture, out var scale)) return null;
        scale = scale <= 0 ? 1.0 : scale;

        // parts[5] = массив monitors; берём первый.
        var firstMonitor = SplitArrayElements(parts[5]).FirstOrDefault();
        if (firstMonitor is null) return null;

        var monFields = SplitTupleElements(firstMonitor);
        if (monFields.Count < 4) return null;

        var connector = Unquote(monFields[0]);
        var vendor = Unquote(monFields[1]).Trim();
        var product = Unquote(monFields[2]).Trim();
        var serial = Unquote(monFields[3]).Trim();

        var id = BuildStableId(vendor, product, serial, connector);

        // Для сшивки с Avalonia важны (X,Y). Ширина/высота здесь не известны без текущего режима.
        var bounds = new ScreenRect(X: x, Y: y, Width: 0, Height: 0);

        return new MonitorIdentity(
            Id: id,
            Connector: connector,
            DisplayName: null,
            LogicalIndex: logicalIndex,
            Bounds: bounds);
    }

    private static string BuildStableId(string vendor, string product, string serial, string connector)
    {
        var hasRealSerial = !string.IsNullOrWhiteSpace(serial)
                            && serial != "0x00000000"
                            && serial != "0";
        if (hasRealSerial && !string.IsNullOrWhiteSpace(vendor))
        {
            return $"{vendor}:{product}:{serial}";
        }
        return connector ?? "unknown";
    }

    private static string Unquote(string token)
    {
        var t = token.Trim();
        if (t.Length >= 2 && t[0] == '\'' && t[^1] == '\'') return t[1..^1];
        if (t.Length >= 2 && t[0] == '"' && t[^1] == '"') return t[1..^1];
        return t;
    }
}
