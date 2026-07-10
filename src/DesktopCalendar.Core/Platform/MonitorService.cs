using DesktopCalendar.Core.Contracts;

namespace DesktopCalendar.Core.Platform;

/// <summary>
/// Композитор: объединяет геометрию из IGeometryProvider (Avalonia) и идентификацию
/// из IMonitorIdentityProvider (платформенный), сопоставляя их по пересечению bounds.
/// Это OS-agnostic ядро IMonitorService — обе платформы используют его как есть,
/// отличаясь только реализацией IMonitorIdentityProvider.
/// </summary>
public sealed class MonitorService : IMonitorService
{
    private readonly IGeometryProvider _geometry;
    private readonly IMonitorIdentityProvider _identity;
    private readonly bool _identityAvailable;

    /// <summary>
    /// </summary>
    /// <param name="geometry">Avalonia-геометрия (всегда есть).</param>
    /// <param name="identity">Платформенная идентификация. Может быть null, если DE не поддерживается
    /// (напр. KDE/XFCE на Linux) — тогда работаем в degraded-режиме: id = DisplayName, выбор не переживёт ребут.</param>
    public MonitorService(IGeometryProvider geometry, IMonitorIdentityProvider? identity)
    {
        _geometry = geometry;
        _identity = identity!;
        _identityAvailable = identity is not null;
    }

    public IReadOnlyList<MonitorInfo> GetMonitors()
    {
        var screens = _geometry.GetScreens();

        if (!_identityAvailable)
        {
            // Degraded: нет стабильного id — используем DisplayName и геометрию напрямую.
            return screens
                .OrderBy(s => s.Bounds.X).ThenBy(s => s.Bounds.Y)
                .Select((s, i) => ToMonitorInfo(
                    id: s.DisplayName ?? $"screen-{i}",
                    connector: null,
                    displayName: s.DisplayName,
                    logicalIndex: i + 1,
                    screen: s))
                .ToList();
        }

        // Нормальный путь: сшиваем identity и geometry по максимальному пересечению bounds.
        var identities = _identity.GetIdentities();
        var usedScreens = new HashSet<ScreenGeometry>();
        var result = new List<MonitorInfo>();

        foreach (var identity in identities.OrderBy(id => id.LogicalIndex))
        {
            // Сшивка: оцениваем совпадение identity и Avalonia-экрана.
            // Используем пересечение bounds; если identity имеет нулевые размеры
            // (Mutter отдаёт только X,Y без разрешения) — fallback на попадание точки.
            var best = screens
                .Where(s => !usedScreens.Contains(s))
                .Select(s => (Screen: s, Score: MatchScore(s.Bounds, identity.Bounds)))
                .Where(t => t.Score > 0)
                .OrderByDescending(t => t.Score)
                .FirstOrDefault();

            if (best.Screen is null)
            {
                // Identity есть, а геометрии нет — монитор отключился или виден только платформенному API.
                // Пропускаем; корректная обработка исчезнувшего монитора — в UI (M2) и silent-режиме (M6).
                continue;
            }

            usedScreens.Add(best.Screen);
            result.Add(ToMonitorInfo(
                id: identity.Id,
                connector: identity.Connector,
                displayName: identity.DisplayName,
                logicalIndex: identity.LogicalIndex,
                screen: best.Screen));
        }

        return result;
    }

    public MonitorInfo? GetById(string id)
        => GetMonitors().FirstOrDefault(m => m.Id == id);

    /// <summary>
    /// Оценка совпадения identity-bounds и Avalonia-screen-bounds.
    /// Возвращает площадь пересечения (>0 = совпали), либо 1 при попадании точки
    /// (X,Y) identity в экран (для случая, когда identity имеет нулевые размеры —
    /// Mutter отдаёт только координаты logical-monitor без разрешения).
    /// 0 = нет совпадения.
    /// </summary>
    private static long MatchScore(ScreenRect screen, ScreenRect identity)
    {
        var inter = screen.Intersect(identity);
        if (inter is { } rect && rect.Area > 0)
        {
            return rect.Area;
        }
        // Fallback: точка (X,Y) identity попадает в экран?
        if (identity.Width <= 0 || identity.Height <= 0
            && identity.X >= screen.X && identity.X < screen.Right
            && identity.Y >= screen.Y && identity.Y < screen.Bottom)
        {
            return 1;
        }
        return 0;
    }

    private static MonitorInfo ToMonitorInfo(
        string id, string? connector, string? displayName, int logicalIndex, ScreenGeometry screen)
    {
        var friendly = BuildFriendlyName(connector, displayName, logicalIndex, screen);
        return new MonitorInfo(
            Id: id,
            LogicalIndex: logicalIndex,
            FriendlyName: friendly,
            BoundsX: screen.Bounds.X,
            BoundsY: screen.Bounds.Y,
            BoundsWidth: screen.Bounds.Width,
            BoundsHeight: screen.Bounds.Height,
            ResolutionWidth: screen.Bounds.Width,
            ResolutionHeight: screen.Bounds.Height,
            IsPrimary: screen.IsPrimary);
    }

    private static string BuildFriendlyName(string? connector, string? displayName, int index, ScreenGeometry screen)
    {
        // "1 — DP-1 (LG Ultra HD)" если есть connector и display-name;
        // "1 — DP-1" если только connector; "1 — LG Ultra HD" если только display-name;
        // "1 — 2560x1440" в degraded-режиме.
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(connector)) parts.Add(connector);
        if (!string.IsNullOrWhiteSpace(displayName)) parts.Add(displayName);
        if (parts.Count == 0) parts.Add($"{screen.Bounds.Width}x{screen.Bounds.Height}");
        return $"{index} — {string.Join(" — ", parts)}";
    }
}
