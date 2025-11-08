using ConsoleAppFramework;
using DryIoc;
using Lilly.Engine.Bootstrap;
using Lilly.Engine.Core.Data.Directories;
using Lilly.Engine.Core.Enums;
using Lilly.Engine.Core.Extensions.Logger;
using Lilly.Engine.Core.Json;
using Lilly.Engine.Lua.Scripting.Context;
using Lilly.Engine.Renderers;
using Lilly.Engine.Rendering.Core.Data.Config;
using Serilog;
using Serilog.Events;

await ConsoleApp.RunAsync(
    args,
    async (
        string rootDirectory = null,
        bool logToFile = false,
        LogLevelType logLevel = LogLevelType.Debug
    ) =>
    {

        JsonUtils.RegisterJsonContext(LillyLuaScriptJsonContext.Default);
        var container = new Container();

        rootDirectory ??= Environment.GetEnvironmentVariable("LILLY_ENGINE_ROOT") ?? Path.Combine(AppContext.BaseDirectory, "lilly");

        var directoriesConfig = new DirectoriesConfig(rootDirectory, Enum.GetNames<AssetType>());

        container.RegisterInstance(directoriesConfig);

        InitializeLogger(logToFile, logLevel.ToSerilogLogLevel(), rootDirectory);

        var bootstrap = new LillyBoostrap(container, new OpenGlRenderer());

        await bootstrap.InitializeAsync(new InitialEngineOptions());

        await bootstrap.RunAsync();

        await bootstrap.ShutdownAsync();
    }
);

void InitializeLogger(bool logToFile, LogEventLevel logEventLevel, string rootDirectory)
{
    var logConfiguration = new LoggerConfiguration();

    logConfiguration.MinimumLevel.Is(logEventLevel);

    logConfiguration.WriteTo.Console();

    if (logToFile)
    {
        logConfiguration.WriteTo.File(
            Path.Combine(rootDirectory, "logs", "lilly-engine-.log"),
            rollingInterval: RollingInterval.Day
        );
    }

    Log.Logger = logConfiguration.CreateLogger();

    Log.Logger.Information("Hi, my name is Lilly!");
}
