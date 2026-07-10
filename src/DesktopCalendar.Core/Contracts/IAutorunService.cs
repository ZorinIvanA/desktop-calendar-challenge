namespace DesktopCalendar.Core.Contracts;

/// <summary>
/// Управление записью автозапуска приложения в ОС.
/// Windows: ключ реестра HKCU\…\Run. Linux: ~/.config/autostart/*.desktop.
/// </summary>
public interface IAutorunService
{
    /// <summary>Находится ли приложение сейчас в списке автозагрузки.</summary>
    bool IsEnabled();

    /// <summary>Добавить в автозагрузку (с указанными аргументами, напр. "-auto").</summary>
    void Enable(string executablePath, string arguments);

    /// <summary>Убрать из автозагрузки.</summary>
    void Disable();
}
