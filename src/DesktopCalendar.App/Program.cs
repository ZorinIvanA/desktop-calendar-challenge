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
    // Handler ничего не возвращает, поэтому код выхода кладём в Environment.ExitCode.
    Environment.ExitCode = auto
        ? RunAuto()
        : RunUi();
}, autoOption);

return await rootCommand.InvokeAsync(args);

// ----------------------------- handlers -----------------------------

static int RunAuto()
{
    // M1: каркас под M6. Реальный silent-пайплайн (применить календарь → выйти) появится в M6.
    Console.WriteLine("auto mode not implemented yet (see M6).");
    return 0;
}

static int RunUi()
{
    var services = ServiceComposition.Build();
    DesktopCalendar.UI.App.ConfigureServices(services);

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
