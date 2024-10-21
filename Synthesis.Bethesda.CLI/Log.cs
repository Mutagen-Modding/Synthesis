using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace Synthesis.Bethesda.CLI;

public static class Log
{
    public static readonly ILogger Logger;

    static Log()
    {
        Serilog.Log.Logger = GetLoggerConfig()
            .WriteTo.Console(theme: ConsoleTheme.None)
            .CreateLogger();

        Logger = Serilog.Log.Logger;
    }

    public static LoggerConfiguration GetLoggerConfig()
    {
        return new LoggerConfiguration()
            .MinimumLevel.Debug();
    }
}