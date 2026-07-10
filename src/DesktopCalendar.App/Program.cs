using System.CommandLine;
using Avalonia;
using DesktopCalendar.App;
using DesktopCalendar.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Разбор аргументов командной строки.
var autoOption = new Option<bool>(
    name: "--auto",
    getDefaultValue: () => false,
    description: "Silent-режим: обновить рабочий стол выбранного монитора и выйти (без UI).");
autoOption.AddAlias("-auto");

var rootCommand = new RootCommand("Desktop Calendar — рисует календарь поверх обоев рабочего стола.");
rootCommand.AddOption(autoOption);

rootCommand.SetHandler((bool auto) =>
{
    Environment.ExitCode = auto ? RunAuto() : RunUi();
}, autoOption);

await rootCommand.InvokeAsync(args);
return Environment.ExitCode;

// ----------------------------- handlers -----------------------------

static int RunAuto()
{
    // Silent-режим M6: без UI, применяет календарь к рабочему столу и выходит.
    var services = ServiceComposition.Build();
    try
    {
        var auto = services.GetRequiredService<DesktopCalendar.Core.Wallpaper.AutoUpdateService>();
        var code = auto.Run();
        // DebouncedSettingsStore пишет отложенно — форсируем flush перед выходом,
        // иначе LastAppliedUtc/LastGeneratedPath не сохранятся (процесс завершится раньше таймера).
        if (services.GetService(typeof(DesktopCalendar.Core.Settings.DebouncedSettingsStore))
            is DesktopCalendar.Core.Settings.DebouncedSettingsStore debounced)
        {
            debounced.Flush();
        }
        return code;
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Auto mode failed to start.");
        Console.Error.WriteLine($"Fatal: {ex}");
        return DesktopCalendar.Core.Wallpaper.AutoUpdateService.ExitError;
    }
}

static int RunUi()
{
    var services = ServiceComposition.Build();
    DesktopCalendar.UI.App.ConfigureServices(services);

    // Один экземпляр GUI: второй запуск выходит молча с логом (M7).
    var paths = services.GetService<DesktopCalendar.Core.Contracts.IPlatformPaths>();
    using var guard = new DesktopCalendar.Core.Platform.SingleInstanceGuard(paths?.AppDataDir ?? "/tmp");
    if (!guard.TryAcquire())
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogWarning("Another instance is already running, exiting.");
        Console.Out.WriteLine("Desktop Calendar уже запущен.");
        return 0;
    }

    try
    {
        AppBuilder.Configure<DesktopCalendar.UI.App>()
                  .UsePlatformDetect()
                  .LogToTrace()
                  .StartWithClassicDesktopLifetime(Environment.GetCommandLineArgs());
        return 0;
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Avalonia desktop lifetime terminated with an error.");
        Console.Error.WriteLine($"Fatal: {ex}");
        return 1;
    }
}
