using System.IO;
using Serilog;

namespace Synthesis.Bethesda.GUI.Logging;

public static class Log
{
    public static readonly ILogger Logger;
    public static readonly DateTime StartTime;
    public const string LogFolder = "logs";

    // File sink default template plus thread id.
    private const string OutputTemplate =
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [T{ThreadId}] {Message:lj}{NewLine}{Exception}";

    static Log()
    {
        StartTime = DateTime.Now;

        if (LogPreferences.IsTesting)
        {
            // Create a dummy logger for testing that doesn't write to files
            Serilog.Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .CreateLogger();

            Logger = Serilog.Log.Logger;
            return;
        }

        var startTime = $"{StartTime:HH_mm_ss}";
        startTime = startTime.Remove(5, 1);
        startTime = startTime.Remove(2, 1);
        startTime = startTime.Insert(2, "h");
        startTime = startTime.Insert(5, "m");
        startTime += "s";
        var logFileName = $"{StartTime:MM-dd-yyyy}_{startTime}.log";

        var curLog = Path.Combine(LogFolder, "Current.log");
        if (File.Exists(curLog))
        {
            File.Delete(curLog);
        }

        Serilog.Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.WithThreadId()
            .WriteTo.File(Path.Combine(LogFolder, logFileName), outputTemplate: OutputTemplate, retainedFileTimeLimit: TimeSpan.FromDays(7))
            .WriteTo.File(curLog, outputTemplate: OutputTemplate)
            .CreateLogger();

        Logger = Serilog.Log.Logger;
    }
}