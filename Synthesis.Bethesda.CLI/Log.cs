using Serilog;

namespace Synthesis.Bethesda.CLI;

public static class Log
{
    public static readonly ILogger Logger;

    static Log()
    {
        Serilog.Log.Logger = GetLoggerConfig()
            .WriteTo.Console()
            .CreateLogger();

        Logger = Serilog.Log.Logger;
    }

    public static LoggerConfiguration GetLoggerConfig()
    {
        return new LoggerConfiguration()
            .MinimumLevel.Debug();
    }
}