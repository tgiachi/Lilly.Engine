using ConsoleAppFramework;
using DryIoc;
using Lilly.Engine.Core.Data.Directories;
using Lilly.Engine.Core.Enums;
using Lilly.Engine.Core.Extensions.Logger;
using Lilly.Engine.Core.Json;
using Lilly.Engine.Core.Logging;
using Lilly.Engine.Lua.Scripting.Context;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

await ConsoleApp.RunAsync(
    args,
    async (
        string rootDirectory = null,
        bool logToFile = false,
        LogLevelType logLevel = LogLevelType.Debug,
        int width = 1280 ,
        int height = 720
    ) =>
    {

        //--root-directory /Users/squid/lilly --width 3272 --height 1277


        JsonUtils.RegisterJsonContext(LillyLuaScriptJsonContext.Default);
        var container = new Container();

        rootDirectory ??= Environment.GetEnvironmentVariable("LILLY_ENGINE_ROOT") ??
                          Path.Combine(AppContext.BaseDirectory, "lilly");

        var directoriesConfig = new DirectoriesConfig(rootDirectory, Enum.GetNames<DirectoryType>());

        container.RegisterInstance(directoriesConfig);

        InitializeLogger(logToFile, logLevel.ToSerilogLogLevel(), rootDirectory);






    }
);

/// <summary>
/// Initializes the Serilog logger with the specified log level, console output, and optional file logging.
/// </summary>
void InitializeLogger(bool logToFile, LogEventLevel logEventLevel, string rootDirectory)
{
    var logConfiguration = new LoggerConfiguration();

    logConfiguration.MinimumLevel.Is(logEventLevel);



    logConfiguration.WriteTo.Async(s => s.Console(theme: AnsiConsoleTheme.Code));
    //logConfiguration.WriteTo.Console(theme: AnsiConsoleTheme.Literate);

    if (logToFile)
    {
        logConfiguration.WriteTo.File(
            Path.Combine(rootDirectory, "logs", "lilly-engine-.log"),
            rollingInterval: RollingInterval.Day
        );
    }

    logConfiguration.Enrich.FromLogContext();

    logConfiguration.WriteTo.EventSink();

    Log.Logger = logConfiguration.CreateLogger();

    Log.Logger.Information("Hi, my name is Lilly!");
}
