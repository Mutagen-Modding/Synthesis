using System.IO;
using Serilog;

namespace Synthesis.Bethesda.GUI.Logging;

public static class Log
{
    public static readonly ILogger Logger;
    public static readonly DateTime StartTime;
    public const string LogFolder = "logs";

    static Log()
    {
        StartTime = DateTime.Now;

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
            .WriteTo.File(Path.Combine(LogFolder, logFileName), retainedFileTimeLimit: TimeSpan.FromDays(7))
            .WriteTo.File(curLog)
            .CreateLogger();

        Logger = Serilog.Log.Logger;
    }
}