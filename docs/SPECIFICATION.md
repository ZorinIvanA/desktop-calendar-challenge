# Спецификация (SPECIFICATION)

**Проект:** Desktop Calendar.
**Сопровождает:** [`TZ.md`](./TZ.md). Этот документ — **как** реализовать требования.
**Дата:** 2026-07-10.

---

## 1. Архитектура высокого уровня

Приложение разбито на ядро (платформонезависимое) и платформенные адаптеры. Связь — через узкие
интерфейсы; UI зависит только от абстракций, что позволяет тестировать логику без обращения к ОС.

```
┌─────────────────────────────────────────────────────────────────┐
│                     DesktopCalendar.UI (Avalonia)               │
│   Views / ViewModels / Preview renderer                         │
└───────────────┬─────────────────────────────────────────────────┘
                │ depends on (abstractions only)
┌───────────────┴─────────────────────────────────────────────────┐
│                    DesktopCalendar.Core                         │
│  - Settings (model + JSON store)                                │
│  - Calendar model + layout calculator                          │
│  - Rendering pipeline (Avalonia.Media / Skia)                  │
│  - Application service (orchestrates apply/remove/preview)     │
│  - Contracts: IMonitorService, IWallpaperService,             │
│               IAutorunService, IPlatformPaths                 │
└───────────────┬─────────────────────────────────────────────────┘
                │ implemented by
        ┌───────┴────────┬──────────────────┐
┌───────▼──────┐ ┌───────▼────────┐ ┌────────▼───────────────┐
│ Windows      │ │ Linux.Gnome    │ │ Linux.Other (stub)     │
│ Platform     │ │ Platform       │ │ throw NotSupported     │
└──────────────┘ └────────────────┘ └────────────────────────┘
```

### 1.1. Проекты в solution

| Проект | Target | Роль |
|---|---|---|
| `DesktopCalendar.Core` | `net8.0` | модель, рендеринг, контракты, оркестрация |
| `DesktopCalendar.UI` | `net8.0` | Avalonia UI (Views/ViewModels), точка входа GUI |
| `DesktopCalendar.Platform.Windows` | `net8.0-windows` | Win32 + COM (`IDesktopWallpaper`, реестр `Run`) |
| `DesktopCalendar.Platform.Linux` | `net8.0` | GNOME (gsettings/dconf/dbus), `*.desktop` autostart, выбор DE |
| `DesktopCalendar.App` (слient entry) | `net8.0` + RID | сборка всего, разбор аргументов, DI-композиция |

UI и Core — OS-agnostic. Платформенные проекты подключаются через DI в `DesktopCalendar.App` на
старте по `OperatingSystem.IsWindows()` / `OperatingSystem.IsLinux()`.

### 1.2. Точка входа и режимы

`Program.Main(string[] args)`:
- Парсит аргументы (`-auto`, `--help`). Используется `System.CommandLine`.
- `-auto` → `AutoUpdateService.Run()` (без UI), выход с кодом.
- без аргументов → запуск Avalonia `AppBuilder`, главное окно.

## 2. Контракты (Core)

```csharp
public interface IMonitorService {
    IReadOnlyList<MonitorInfo> GetMonitors();                 // в порядке ОС
    MonitorInfo? GetById(string id);
}

public sealed record MonitorInfo(
    string Id,                 // стабильный: Windows device path / Linux connector name
    int LogicalIndex,          // 1-based, как видит пользователь
    string FriendlyName,
    PixelRect Bounds,          // в виртуальных координатах рабочего стола
    PixelSize Resolution);     // физическое разрешение

public interface IWallpaperService {
    WallpaperSnapshot GetCurrent(string monitorId);           // что сейчас стоит
    void SetWallpaper(string monitorId, string imageFilePath);
    string? GetOriginalPath(string monitorId);                // сохранённый оригинал
    WallpaperFit ParseCurrentFit(string monitorId);           // режим растяжения ОС
}

public sealed record WallpaperSnapshot(
    string CurrentUri,          // что сейчас стоит (path/URI)
    string? LastGeneratedPath,  // наш последний сгенерированный файл (если был)
    bool HasCalendar);          // сейчас стоит наш файл с календарём?

public interface IAutorunService {
    bool IsEnabled();
    void Enable(string executablePath, string arguments);     // arguments = "-auto"
    void Disable();
}

public interface IPlatformPaths {
    string AppDataDir { get; }        // %LOCALAPPDATA%\DesktopCalendar / ~/.local/share/DesktopCalendar
    string OriginalsDir { get; }      // кэш исходных обоев
    string GeneratedDir { get; }      // текущие сгенерированные файлы
    string SettingsFile { get; }      // settings.json
    string LogFile { get; }
}

public enum WallpaperFit { Fill, Fit, Stretch, Center, Tile, Span }
```

`HasCalendar` определяется сравнением текущего URI с `LastGeneratedPath` (см. §5.3).

## 3. Модель настроек

```csharp
public sealed class AppSettings {
    public string? TargetMonitorId { get; set; }
    public Anchor Anchor { get; set; } = Anchor.BottomRight;
    public int MarginPx { get; set; } = 32;

    public string FontFamily { get; set; } = "Segoe UI";      // платформа подставляет дефолт
    public double FontSize { get; set; } = 28;                 // pt

    public PresetColor ColorMonth    { get; set; } = PresetColor.White;
    public PresetColor ColorWeekday  { get; set; } = PresetColor.White;
    public PresetColor ColorDay      { get; set; } = PresetColor.White;
    public PresetColor? ColorToday   { get; set; }
    public PresetColor? ColorWeekend { get; set; }

    public OtherMonthMode OtherMonthMode { get; set; } = OtherMonthMode.Show;
    public byte OtherMonthOpacity { get; set; } = 100;         // 0..100, при Opacity-режиме
    public bool HideOtherMonth { get; set; } = false;          // альтернатива: bool

    public LabelsPosition LabelsPosition { get; set; } = LabelsPosition.Top;

    public bool Autorun { get; set; } = false;

    // служебные
    public string? LastOriginalPath { get; set; }   // путь к сохранённому оригиналу (по монитору)
    public string? LastGeneratedPath { get; set; }  // последний сгенерированный файл
    public DateTime? LastAppliedUtc { get; set; }
}

public enum Anchor {
    TopLeft, TopCenter, TopRight,
    CenterLeft, CenterRight,
    BottomLeft, BottomCenter, BottomRight
}

public enum PresetColor { Red, Green, Blue, White }

public enum OtherMonthMode { Show, Hide, CustomOpacity }

public enum LabelsPosition { Top, Bottom }
```

> Палитра RGB для предустановленных цветов (фиксируется здесь, единая для всех ОС):
> `Red = #E53935`, `Green = #43A047`, `Blue = #1E88E5`, `White = #FFFFFF`. Цвета взяты из
> Material-палитры ради хорошей читаемости на произвольных фотообоях; при необходимости меняются в
> одном месте.

### 3.1. Хранение

- Формат — `settings.json` (System.Text.Json, indented, UTF-8).
- Путь — `IPlatformPaths.SettingsFile`.
- Запись — debounced (500 мс после последнего изменения свойства), чтобы не писать файл на каждый
  чекбокс. Реализация — `DebouncedSettingsStore`.
- Чтение — при старте; если файл отсутствует/повреждён — значения по умолчанию + лог warning.
- **Реестр Windows для настроек не используется.** Реестр задействован только под автозапуск.

## 4. Платформенные реализации

### 4.1. Windows

| Задача | API |
|---|---|
| Мониторы | `IDesktopWallpaper.GetMonitorDevicePathCount/At`, `GetMonitorDeviceRectAt`, `GetMonitorRECT` |
| Текущие обои | `IDesktopWallpaper.GetWallpaper(monitorId)` |
| Установка | `IDesktopWallpaper.SetWallpaper(monitorId, path)` |
| Режим растяжения | `IDesktopWallpaper.Get/SetPosition` (`DWPOS_FILL/FIT/STRETCH/CENTER/SPAN`) |
| Автозапуск | `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` значение `DesktopCalendar` = `"<exe>" -auto` |

COM-интерфейс `IDesktopWallpaper` (CLSID `{C2CF3110-460E-4fc1-B9D0-8A1C0C9CC4BD}`, IID
`{B92B56A9-8B55-4E14-9A89-0199BBB6F93B}`) объявляется через `[ComImport]` вручную. Используется
пер-monitor API — это закрывает требование «обновляется только один монитор».

### 4.2. Linux / GNOME

| Задача | API |
|---|---|
| Мониторы | `Gdk.DisplayManager` (через Avalonia) + `Gdk.Monitor` для геометрии; стабильный id — `connector` из `Gdk.Monitor.Connector` (Wayland) либо `xrandr` name (X11) |
| Текущие обои | `gsettings get org.gnome.desktop.background picture-uri` (и `picture-uri-dark` для тёмной темы) |
| Установка | `gsettings set org.gnome.desktop.background picture-uri 'file://…'` (аналогично `-dark`), опционально `picture-options` |
| Режим растяжения | `gsettings get org.gnome.desktop.background picture-options` (`zoom`/`scaled`/`stretched`/`centered`/`wallpaper`/`spanned`) |
| Автозапуск | `~/.config/autostart/desktopcalendar.desktop` с `Exec=<exe> -auto`, `Terminal=false`, `X-GNOME-Autostart-enabled=true` |

Реализация — через `Process.Start("gsettings", …)` как самый стабильный путь (не требует dbus-биндингов
и работает и на X11, и на Wayland). Альтернативно — прямой dconf через `dbus-send`; выбор закреплён в
коде через `GnomeWallpaperService`.

**Per-monitor ограничение GNOME (см. TZ §2.2):** `SetWallpaper(monitorId, …)` игнорирует monitorId
на уровне ОС, но приложение продолжает: 1) хранит оригинал по monitorId, 2) рисует календарь по
координатам выбранного монитора, 3) выставляет единый URI. В UI показывается поясняющая надпись при
выборе монитора в GNOME.

### 4.3. Linux / прочие DE

`XDG_CURRENT_DESKTOP` определяет выбор backend'а. Поддерживаемые значения `gnome`/`ubuntu` →
`GnomeWallpaperService`. Иначе — заглушка, выбрасывающая `PlatformNotSupportedException` с
понятным сообщением в UI и логе. Контракт `IWallpaperService` позволяет добавить KDE/XFCE/Cinnamon/MATE
без изменений в Core/UI.

## 5. Рендеринг и геометрия (самая чувствительная часть)

### 5.1. Принцип единого визуального размера

Календарь рисуется в **координатах пикселей монитора**, не исходного файла. Алгоритм:

1. Прочитать режим растяжения ОС (`WallpaperFit`).
2. Загрузить **оригинал** в память (`SkiaSharp.SKBitmap`).
3. Применить к оригиналу тот же трансформ, что делает ОС при показе, получить `monitorCanvas`
   размером `MonitorInfo.Resolution` (например, 2560×1440). Это «то, что видит пользователь как фон».
4. Рассчитать прямоугольник календаря в координатах `monitorCanvas` (см. §5.2).
5. Нарисовать календарь в этот прямоугольник (Skia).
6. Сохранить результат как новый файл — он становится «видимым» фоном.
7. Установить файл как обои в режиме **`Stretch`/`Fill`/`Center`** по выбору (рекомендуется `Center`,
   чтобы ОС не масштабировала уже подготовленный 1:1 канвас; см. §5.4).

Так как итоговый файл имеет пиксельный размер = разрешению монитора, а режим показа = `Center` (1:1),
**календарь всегда выглядит одинаково**, независимо от того, как ОС раньше растягивала оригинал.

### 5.2. Расчёт прямоугольника календаря

```
measureSize = MeasureCalendar(fontFamily, fontSize, 7 columns)   // Skia MeasureText + padding
rect.width  = measureSize.Width
rect.height = measureSize.Height

switch (Anchor) {
  TopLeft:     rect.x = marginPx;            rect.y = marginPx;
  TopCenter:   rect.x = (W - rect.width)/2;  rect.y = marginPx;
  TopRight:    rect.x = W - rect.width - m;  rect.y = marginPx;
  CenterLeft:  rect.x = marginPx;            rect.y = (H - rect.height)/2;
  CenterRight: rect.x = W - rect.width - m;  rect.y = (H - rect.height)/2;
  BottomLeft:  rect.x = marginPx;            rect.y = H - rect.height - m;
  BottomCenter:rect.x = (W - rect.width)/2;  rect.y = H - rect.height - m;
  BottomRight: rect.x = W - rect.width - m;  rect.y = H - rect.height - m;
}
// где W,H — Resolution выбранного монитора, m — MarginPx
```

### 5.3. Модель календаря и отрисовка

- Неделя начинается с понедельника. Названия дней — фиксированные `["Mo","Tu","We","Th","Fr","Sa","Su"]`.
- Таблица — 6 строк × 7 столбцов (достаточно для любого месяца). В первой строке — дни конца
  предыдущего месяца до 1-го числа текущего; в последних строках — дни начала следующего.
- Порядок отрисовки блоков внутри прямоугольника зависит от `LabelsPosition`:
  - `Top`: [название месяца] → [дни недели] → [таблица дней]
  - `Bottom`: [таблица дней] → [дни недели] → [название месяца]
- Цвет ячейки: `ColorToday` (если день = сегодня) > `ColorWeekend` (если Sa/Su и день текущего
  месяца) > `ColorDay`. Название месяца → `ColorMonth`, дни недели → `ColorWeekday`.
- Дни «не текущего месяца»: по `OtherMonthMode` — `Show` (как обычные, цвет `ColorDay`),
  `Hide` (не рисуются), `CustomOpacity` (рисуются с альфой `OtherMonthOpacity`/100).

Превью рендерится тем же `CalendarRenderer` (см. §6), чтобы превью = финал.

### 5.4. Режим показа результата

При установке сгенерированного файла приложение также выставляет режим показа:
- Windows — `DWPOS_CENTER` (или `DWPOS_FILL`, но тогда 1:1 файл не искажается, т.к. уже под разрешение).
- GNOME — `picture-options = 'wallpaper'`→ нет; используем `'center'` либо `'spanned'` для
  single-monitor-псевдо-multi. Рекомендуется `'zoom'` если разрешение файла = разрешению экрана
  (фактически 1:1). Финал фиксируется в `WallpaperApplier`.

### 5.5. Сохранность оригинала и очистка

- При первом применении к монитору читается `GetCurrent(monitorId).CurrentUri`, копируется в
  `OriginalsDir/<monitorId>.<ext>`. Это — «оригинал», который не трогается.
- Каждая перерисовка: новый файл пишется в `GeneratedDir/<monitorId>_<yyyy-MM-dd>.png`, после
  успешной установки **все** прежние файлы из `GeneratedDir` для этого монитора, кроме только что
  созданного, удаляются.
- «Убрать календарь»: установить в качестве обоев файл из `OriginalsDir/<monitorId>.<ext>` и
  восстановить исходный `WallpaperFit`, ранее сохранённый в настройках/снимке.
- При потере/переписывании оригинала (пользователь сменил обои извне) — `OriginalsDir`
  обновляется по факту: если `CurrentUri != LastGeneratedPath && CurrentUri != LastOriginalPath`,
  считается, что пользователь сменил обои вручную → новый оригинал подхватывается автоматически.
  Это покрывает «календарь поверх актуального оригинала каждый день».

### 5.6. Диаграмма состояний рабочего стола

```
              [оригинал установлен пользователем/ОС]
                          │
            apply()       │       remove()
        ┌─────────────────▼─────────────────┐
        │  generated.png (с календарём)     │
        │  LastGeneratedPath = generated.png│
        └───────────────────────────────────┘
HasCalendar = (CurrentUri == LastGeneratedPath)
→决定了UI显示哪颗按钮: true → «Убрать»; false → «Установить».
```

## 6. Превью (UI)

- В разделе «Настройки календаря» — `PreviewControl`.
- Берётся фрагмент `monitorCanvas` (§5.1, шаг 3) с запасом вокруг расчётного `rect` (например,
  `rect` + 2×padding, но не больше разрешения). Если превью не помещается в окно — масштабируется с
  сохранением пропорций, **но календарь внутри рисуется в натуральную величину до общего
  масштабирования фрагмента**, чтобы пользователь видел реальные пропорции шрифта/ячеек.
- Превью обновляется реактивно: ViewModels пробрасывают `AppSettings` в `CalendarRenderer`, который
  возвращает `SKBitmap`/`WriteableBitmap` для отрисовки через `SKBitmap` → Avalonia `Bitmap`.

## 7. UI/UX дизайн

### 7.1. Каркас окна

Одно окно с вертикальной структурой:
1. **Шапка:** название приложения, индикатор платформы/DE (например, «Windows» или «GNOME on
   Wayland», и предупреждение, если DE не поддерживается).
2. **Левая колонка — навигация по разделам** (TabControl или `ListBox`-sidebar):
   - Монитор
   - Расположение
   - Календарь
   - Общие
3. **Правая колонка — содержимое раздела + превью** (превью виден всегда внизу/сбоку, кроме раздела
   «Монитор», где сам выбор монитора визуален).

### 7.2. Раздел «Монитор»

- Визуальная схема: прямоугольники мониторов, расположенные согласно их `Bounds` (виртуальный
  рабочий стол), с подписями вида `1 — DP-1 (2560×1440)`. Порядок и номера — из ОС (`LogicalIndex`).
- Клик по прямоугольнику выбирает `TargetMonitorId`.
- Под схемой — текстовая расшифровка выбранного монитора.
- В GNOME — поясняющая плашка про ограничение per-screen (см. §4.2).

### 7.3. Раздел «Расположение»

- Слева — квадратная мини-схема экрана с 8 кликабельными «точками» привязки (радио-кнопки,
  наложенные на схему).
- Справа — поле «Отступ от края, px» (NumericUpDown, min 0, шаг 1).
- Снизу — инклюзивное превью.

### 7.4. Раздел «Календарь»

Сгруппировано:
- **Шрифт:** выпадающий список `FontFamily` (источник — `InstalledFontCollection` Skia /
  `SystemFontCollection` Avalonia) + `FontSize` (NumericUpDown, pt).
- **Цвета:** три строки `Month / Weekday / Days` — каждая ComboBox из `{White, Red, Green, Blue}`.
- **Опциональные цвета:** чекбокс «Выделять сегодняшний день» + ComboBox; чекбокс «Выделять
  выходные» + ComboBox. При снятии чекбокса ComboBox дизейблится и в настройки пишется `null`.
- **Дни соседних месяцев:** RadioButton `Показывать / Скрывать / Прозрачность`, при последнем —
  ползунок 0–100.
- **Подписи:** RadioButton `Сверху / Снизу`.
- Справа/снизу — большое превью на фоне реального фрагмента.

### 7.5. Раздел «Общие»

- Чекбокс «Запускать вместе с системой» (=`Autorun`). Переключение дёргает `IAutorunService`.
- Две кнопки, **из которых активна ровно одна** в данный момент:
  - «Установить календарь сейчас» — активна, когда `HasCalendar == false`.
  - «Убрать календарь» — активна, когда `HasCalendar == true`.
  Состояние запрашивается при открытии раздела и после каждого действия.
- Кнопка «Открыть папку данных» (диагностика).
- Информация о версии / ссылки.

### 7.6. Реактивность и сохранение

- ViewModel реализует `INotifyPropertyChanged` (через `CommunityToolkit.Mvvm` `ObservableObject`).
- Подписка на изменения свойств → `DebouncedSettingsStore.ScheduleSave()` (500 мс) + обновление
  превью (немедленно, throttled 50 мс).
- Долгие операции (чтение/запись обоев) — асинхронно (`async/await`), с блокировкой UI и
  индикатором, чтобы не «вешать» окно.

## 8. Silent-режим

`AutoUpdateService.Run()`:
1. Загрузить настройки. Если `TargetMonitorId == null` — лог + код `2`.
2. Проверить, не исчез ли выбранный монитор (`IMonitorService.GetById`). Если исчез — код `3`.
3. Вычислить, нужно ли перерисовывать: если `LastAppliedUtc` приходится на сегодня И текущие обои
   = нашему последнему файлу — выход `0` без работы (идемпотентность, полезно для частого запуска).
4. Иначе — отработать пайплайн применения (§5), записать `LastAppliedUtc`, выход `0`.
5. Любое исключение → лог + код `1`.

Логирование — в `IPlatformPaths.LogFile`, ротация по размеру (Serilog с `RollingFile`, уровень Info
по умолчанию, Debug флагом `-verbose`).

## 9. Управление автозапуском

- Windows: ключ реестра `HKCU\…\Run\DesktopCalendar`. Не требует прав администратора (per-user).
- Linux: `~/.config/autostart/desktopcalendar.desktop`, поля `Type=Application`, `Name=Desktop
  Calendar`, `Exec=<abs exe> -auto`, `Icon=` (опц.), `X-GNOME-Autostart-enabled=true`,
  `Hidden=false`.
- Чекбокс «в автозагрузке» синхронизирован с реальным состоянием: при открытии UI значение
  запрашивается у `IAutorunService.IsEnabled()`; галочка — это и есть источник правды.

## 10. Обработка угловых случаев

| Случай | Поведение |
|---|---|
| Выбранный монитор отключён во время работы | UI: плашка «монитор недоступен», кнопка применить заблокирована. Silent: код 3, лог. |
| Сменили обои извне | `OriginalsDir` обновляется автоматически (§5.5), календарь рисуется на новом оригинале. |
| Переключение тёмной темы (GNOME) | Читаем/пишем оба `picture-uri` и `picture-uri-dark`. |
| Wayland vs X11 | `gsettings` работает в обоих; id монитора берём из `Gdk.Monitor` в Avalonia (connector). |
| Оригинал =URI недоступен (сетевой/удалён) | Падать с понятной ошибкой; в UI — сообщение. |
| Файл сгенерированного календаря слишком велик | Сохраняем PNG без потерь; при превышении разумного порога — лог warning. |
| Несколько запусков UI одновременно | `Mutex` (Windows) / lock-файл в `AppDataDir` (Linux): второй экземпляр активирует первый и выходит. |

## 11. Безопасность и приватичность

- Все операции локальные; в сеть приложение не ходит.
- Нет доступа к контактам/файлам пользователя, кроме: чтение текущего URI обоев, чтение оригинала,
  запись в собственный `AppDataDir`, запись настроек ОС (реестр `Run` / `gsettings` / autostart).
- Логи не содержат чувствительных данных (только пути, id мониторов, ошибки).

## 12. Тестирование

- **Unit-тесты** (`DesktopCalendar.Core.Tests`, xUnit):
  - расчёт `Anchor` → `rect` для всех 8 значений и нескольких разрешений,
  - цветовая приоритизация (today > weekend > day),
  - `OtherMonthMode` и opacity,
  - модель месяца (корректные числа соседних дней для разных месяцев),
  - идемпотентность `AutoUpdateService` (через mock-сервисы),
  - cleanup старых сгенерированных файлов.
- **Контрактные тесты** на `IWallpaperService`/`IMonitorService` — выполняются вручную на каждой ОС
  (чек-лист в §13).
- Платформенный код вынесен за тонкие адаптеры, чтобы Core был полностью тестируем в CI на Linux.

## 13. Чек-лист приёмочных тестов (по платформам)

### Windows
1. [ ] 2 монитора, разные обои → календарь применяется только на выбранный.
2. [ ] Смена дня → silent-режим перерисовывает.
3. [ ] «Убрать календарь» → исходные обои восстановлены, режим растяжения исходный.
4. [ ] Чекбокс автозапуска → запись в `HKCU\…\Run` появляется/исчезает.
5. [ ] Настройки переживают перезапуск.

### Linux (GNOME, Ubuntu)
6. [ ] Список мониторов соответствует `xrandr`/Settings.
7. [ ] Применить → `gsettings get picture-uri` указывает на наш файл.
8. [ ] «Убрать» → URI восстановлен в исходный.
9. [ ] `~/.config/autostart/desktopcalendar.desktop` создаётся/удаляется по чекбоксу.
10. [ ] Работает и на X11, и на Wayland сессии.

## 14. Этапы разработки (milestones)

| # | Этап | Результат |
|---|---|---|
| M1 | Каркас solution, DI, контракты, `IPlatformPaths`, JSON-настройки | Запускается пустое Avalonia-окно; настройки сохраняются/грузятся |
| M2 | `IMonitorService` (Win + GNOME) + раздел UI «Монитор» | Список мониторов рисуется корректно на обеих ОС |
| M3 | `CalendarRenderer` (Skia) + модель месяца + расчёт геометрии | Unit-тесты зелёные; рендер календаря в `SKBitmap` |
| M4 | `IWallpaperService` (Win per-monitor + GNOME) + пайплайн применения/убирания | На обеих ОС календарь появляется и убирается; оригинал не теряется |
| M5 | Превью в UI, разделы «Расположение»/«Календарь»/«Общие» | Полный UI по ТЗ, превью реактивно |
| M6 | `-auto` + `IAutorunService` + идемпотентность + cleanup | Silent-режим, автозапуск, очистка старых файлов |
| M7 | Угловые случаи, тёмная тема GNOME, lock-файл, полировка UX | Приёмочные чек-листы §13 закрыты |

## 15. Открытые вопросы / риски

- **KDE/XFCE backend'ы** не в scope (по решению). Если позже понадобится — добавляются как
  реализации `IWallpaperService` без правок ядра.
- **Wayland-специфика id монитора** на разных версиях GNOME может отличаться; заложен `connector`,
  но потребуется проверка на целевой Ubuntu 24.04.
- **Тёмная/светлая тема GNOME** — пишем в оба ключа; если пользователь использует расширения вроде
  *Wallpaper Slideshow*, поведение может конфликтовать — документируем как known limitation.
