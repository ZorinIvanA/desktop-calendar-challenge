using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace DesktopCalendar.Core.Settings;

/// <summary>
/// Хранилище настроек в settings.json (System.Text.Json, indented, UTF-8).
/// Чтение устойчиво к отсутствию/повреждению файла — возвращает дефолт.
/// </summary>
public sealed class JsonSettingsStore : ISettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        // Enum как строки — settings.json человекочитаемый ("anchor": "BottomRight" вместо 7).
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private readonly string _filePath;
    private readonly ILogger<JsonSettingsStore> _logger;

    public JsonSettingsStore(string filePath, ILogger<JsonSettingsStore> logger)
    {
        _filePath = filePath;
        _logger = logger;
    }

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                _logger.LogInformation("Settings file not found, using defaults: {Path}", _filePath);
                return CreateDefaults();
            }

            var json = File.ReadAllText(_filePath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
            if (settings is null)
            {
                _logger.LogWarning("Settings file deserialized to null, using defaults: {Path}", _filePath);
                return CreateDefaults();
            }
            return settings;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read settings, using defaults: {Path}", _filePath);
            return CreateDefaults();
        }
    }

    /// <summary>Дефолтные настройки с OS-специфичным шрифтом (для первого запуска).</summary>
    private static AppSettings CreateDefaults() => new()
    {
        FontFamily = DesktopCalendar.Core.Platform.PlatformDefaults.DefaultFontFamily,
    };

    public void Save(AppSettings settings)
    {
        try
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var json = JsonSerializer.Serialize(settings, JsonOptions);

            // Атомарная запись через temp + rename, чтобы не получить полу-записанный файл.
            var tmp = _filePath + ".tmp";
            File.WriteAllText(tmp, json);
            if (File.Exists(_filePath))
            {
                File.Replace(tmp, _filePath, destinationBackupFileName: null);
            }
            else
            {
                File.Move(tmp, _filePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings: {Path}", _filePath);
            throw;
        }
    }
}
