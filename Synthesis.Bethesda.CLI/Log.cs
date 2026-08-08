using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace Synthesis.Bethesda.CLI;

public static class Log
{
    public static readonly ILogger Logger;

    // Console default template plus thread id.
    private const string OutputTemplate =
        "[{Timestamp:HH:mm:ss} {Level:u3}] [T{ThreadId}] {Message:lj}{NewLine}{Exception}";

    static Log()
    {
        Serilog.Log.Logger = GetLoggerConfig()
            .WriteTo.Console(theme: ConsoleTheme.None, outputTemplate: OutputTemplate)
            .CreateLogger();

        Logger = Serilog.Log.Logger;
    }

    public static LoggerConfiguration GetLoggerConfig()
    {
        return new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.WithThreadId();
    }
}