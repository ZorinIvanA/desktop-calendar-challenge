using System.Text.Json.Serialization;
using DesktopCalendar.Core.Contracts;

namespace DesktopCalendar.Core.Settings;

/// <summary>
/// Полная модель настроек приложения. Сохраняется в settings.json (SPECIFICATION §3).
/// </summary>
public sealed class AppSettings
{
    // --- Целевой монитор ---

    /// <summary>Стабильный идентификатор монитора в терминах ОС. Null, пока не выбран.</summary>
    public string? TargetMonitorId { get; set; }

    // --- Расположение календаря ---

    /// <summary>Одна из 8 точек привязки.</summary>
    public Anchor Anchor { get; set; } = Anchor.BottomRight;

    /// <summary>Отступ от края экрана в пикселях (&gt;= 0).</summary>
    public int MarginPx { get; set; } = 32;

    // --- Шрифт ---

    /// <summary>Семейство шрифта из числа установленных в ОС.</summary>
    public string FontFamily { get; set; } = "Segoe UI";

    /// <summary>Размер шрифта в pt.</summary>
    public double FontSize { get; set; } = 28;

    // --- Цвета ---

    public PresetColor ColorMonth { get; set; } = PresetColor.White;
    public PresetColor ColorWeekday { get; set; } = PresetColor.White;
    public PresetColor ColorDay { get; set; } = PresetColor.White;

    /// <summary>Опц. цвет сегодняшнего дня. Null — не выделять.</summary>
    public PresetColor? ColorToday { get; set; }

    /// <summary>Опц. цвет выходных (Sa/Su текущего месяца). Null — не выделять.</summary>
    public PresetColor? ColorWeekend { get; set; }

    // --- Дни соседних месяцев ---

    public OtherMonthMode OtherMonthMode { get; set; } = OtherMonthMode.Show;

    /// <summary>Прозрачность 0..100 при OtherMonthMode == CustomOpacity.</summary>
    public byte OtherMonthOpacity { get; set; } = 100;

    // --- Подписи ---

    public LabelsPosition LabelsPosition { get; set; } = LabelsPosition.Top;

    // --- Общие ---

    /// <summary>Находится ли приложение в списке автозагрузки ОС.</summary>
    public bool Autorun { get; set; }

    // --- Служебное (не редактируется пользователем напрямую) ---

    /// <summary>Путь к сохранённому оригиналу обоев выбранного монитора.</summary>
    public string? LastOriginalPath { get; set; }

    /// <summary>Путь к последнему сгенерированному файлу с календарём.</summary>
    public string? LastGeneratedPath { get; set; }

    /// <summary>Когда последний раз применяли календарь (UTC).</summary>
    public DateTime? LastAppliedUtc { get; set; }

    /// <summary>Разрешение выбранного монитора на момент выбора (для silent-режима без UI/Avalonia).</summary>
    public int? LastMonitorWidth { get; set; }
    public int? LastMonitorHeight { get; set; }

    /// <summary>DPI-scaling выбранного монитора (1.0=96dpi, 2.0=192dpi). Для pt→px в silent-режиме.</summary>
    public double? LastMonitorScaling { get; set; }

    /// <summary>Сохранённый режим растяжения ОС для восстановления при "Убрать календарь".</summary>
    public WallpaperFit? OriginalFit { get; set; }
}
