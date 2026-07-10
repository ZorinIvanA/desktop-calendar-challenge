using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;
using DesktopCalendar.Core.Platform;

namespace DesktopCalendar.UI.Platform;

/// <summary>
/// Источник геометрии экранов через Avalonia TopLevel.Screens.All.
/// Ленивый: TopLevel доступен только после создания окна, поэтому резолвим его
/// в момент вызова GetScreens(), а не в конструкторе.
/// </summary>
public sealed class AvaloniaScreenGeometryProvider : IGeometryProvider
{
    public IReadOnlyList<ScreenGeometry> GetScreens()
    {
        var topLevel = GetTopLevel();
        if (topLevel is null)
        {
            // До создания окна (напр. в silent-режиме M6) геометрия недоступна.
            // monitor id там уже сохранён, и она не нужна — возвращаем пусто.
            return Array.Empty<ScreenGeometry>();
        }

        var screens = topLevel.Screens;
        if (screens is null || screens.ScreenCount == 0)
        {
            return Array.Empty<ScreenGeometry>();
        }

        return screens.All.Select(Map).ToList();
    }

    private static TopLevel? GetTopLevel()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            && desktop.MainWindow is TopLevel tl)
        {
            return tl;
        }
        return null;
    }

    private static ScreenGeometry Map(Screen s)
    {
        // Avalonia Screen.Bounds/WorkingArea — PixelRect (device pixels, virtual desktop coords).
        // На Wayland Scaling всегда 1.0, bounds — logical; для M2 (схема/выбор) это допустимо.
        // Учёт device-vs-logical для рендера обоев — в M3/M4 через Mutter scale.
        return new ScreenGeometry(
            Bounds: new ScreenRect(s.Bounds.X, s.Bounds.Y, s.Bounds.Width, s.Bounds.Height),
            WorkingArea: new ScreenRect(s.WorkingArea.X, s.WorkingArea.Y, s.WorkingArea.Width, s.WorkingArea.Height),
            Scaling: s.Scaling,
            IsPrimary: s.IsPrimary,
            DisplayName: s.DisplayName);
    }
}
