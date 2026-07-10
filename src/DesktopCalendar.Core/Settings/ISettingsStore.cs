using DesktopCalendar.Core.Settings;

namespace DesktopCalendar.Core.Settings;

/// <summary>
/// Доступ к настройкам приложения: синхронное чтение + запись (с debounce).
/// </summary>
public interface ISettingsStore
{
    /// <summary>Загрузить настройки. Никогда не бросает — при ошибке возвращает дефолт.</summary>
    AppSettings Load();

    /// <summary>Сохранить настройки. Реализация может debounce'ить запись.</summary>
    void Save(AppSettings settings);
}
