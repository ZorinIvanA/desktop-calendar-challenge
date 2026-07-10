using System.Diagnostics;

namespace DesktopCalendar.Platform.Linux;

/// <summary>
/// Тонкая обёртка над утилитой gsettings (GNOME).
/// Все вызовы синхронные, через Process.Start. Значения возвращаются "как есть" из stdout:
/// get → GVariant-строка (с кавычками для строковых ключей); set принимает GVariant-строку.
/// </summary>
public sealed class GnomeGsettings
{
    private const string Schema = "org.gnome.desktop.background";
    private const int TimeoutMs = 3000;

    public string? GetPictureUri() => Get(Schema, "picture-uri");
    public string? GetPictureUriDark() => Get(Schema, "picture-uri-dark");
    public string? GetPictureOptions() => Get(Schema, "picture-options");

    public void SetPictureUri(string gvariantUri)
    {
        Set(Schema, "picture-uri", gvariantUri);
        // Тёмная тема: пишем оба ключа, чтобы календарь пережил переключение light/dark.
        Set(Schema, "picture-uri-dark", gvariantUri);
    }

    public void SetPictureOptions(string gvariantOptions) => Set(Schema, "picture-options", gvariantOptions);

    /// <summary>Текущая цветовая схема GNOME (для диагностики; M4 не критично — пишем оба URI).</summary>
    public string? GetColorScheme() => Get("org.gnome.desktop.interface", "color-scheme");

    private static string? Get(string schema, string key)
    {
        var (stdout, _) = Run("get", schema, key, value: null);
        return string.IsNullOrWhiteSpace(stdout) ? null : stdout.Trim();
    }

    private static void Set(string schema, string key, string gvariantValue)
        => Run("set", schema, key, gvariantValue);

    private static (string stdout, string stderr) Run(string verb, string schema, string key, string? value)
    {
        var args = new List<string> { verb, schema, key };
        if (value is not null) args.Add(value);

        var psi = new ProcessStartInfo
        {
            FileName = "gsettings",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var a in args) psi.ArgumentList.Add(a);

        using var proc = Process.Start(psi);
        if (proc is null) return (string.Empty, "gsettings not started");
        if (!proc.WaitForExit(TimeoutMs))
        {
            try { proc.Kill(); } catch { /* ignore */ }
            return (string.Empty, "timeout");
        }
        return (proc.StandardOutput.ReadToEnd(), proc.StandardError.ReadToEnd());
    }
}
