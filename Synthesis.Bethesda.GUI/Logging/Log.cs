using System;
using System.IO;
using Serilog;
using Serilog.Events;
using Synthesis.Bethesda.Execution.Logging;

namespace Synthesis.Bethesda.GUI.Logging
{
    public static class Log
    {
        public static readonly ILogger Logger;
        public static readonly DateTime StartTime;
        public const string OverviewLogName = "--- Overview.txt";
        public const string LogFolder = "logs";
        public const string DateFormat = "MM-dd-yyyy";

        static Log()
        {
            StartTime = DateTime.Now;
            Serilog.Log.Logger = GetLoggerConfig()
                .WriteToFile()
                .CreateLogger();

            Logger = Serilog.Log.Logger;
        }

        public static LoggerConfiguration GetLoggerConfig()
        {
            return new LoggerConfiguration()
                .MinimumLevel.Debug();
        }

        public static LoggerConfiguration WriteToFile(this LoggerConfiguration conf, params string[] extraIdentifiers)
        {
            var startTime = $"{StartTime:HH_mm_ss}";
            startTime = startTime.Remove(5, 1);
            startTime = startTime.Remove(2, 1);
            startTime = startTime.Insert(2, "h");
            startTime = startTime.Insert(5, "m");
            startTime += "s";
            var prefix = Path.Combine(
                LogFolder,
                $"{StartTime:MM-dd-yyyy}",
                $"{startTime}");
            return conf
                .WriteTo.File(
                    formatter: new ExtraAttributeFormatter(),
                    Path.Combine(prefix, $"Everything.txt"))
                .WriteTo.Map(
                    FunnelNames.Profile,
                    default(string?),
                    (profileName, profileWrite) =>
                    {
                        profileWrite.Map(
                            FunnelNames.Patcher,
                            default(string?),
                            (patcherName, patcherWrite) =>
                            {
                                patcherWrite.Map(
                                    FunnelNames.Run,
                                    default(string?),
                                    (run, finalWrite) =>
                                    {
                                        var prefixToUse = prefix;
                                        if (profileName != null)
                                        {
                                            prefixToUse = Path.Combine(prefixToUse, profileName);
                                        }
                                        if (run != null)
                                        {
                                            finalWrite.File(
                                                formatter: new ExtraAttributeFormatter(FunnelNames.Run, FunnelNames.Profile),
                                                Path.Combine(prefixToUse, $"{run}.txt"));
                                            finalWrite.File(
                                                formatter: new ExtraAttributeFormatter(FunnelNames.Profile),
                                                Path.Combine(prefixToUse, OverviewLogName),
                                                restrictedToMinimumLevel: LogEventLevel.Warning);
                                        }
                                        else if (patcherName != null)
                                        {
                                            finalWrite.File(
                                                formatter: new ExtraAttributeFormatter(FunnelNames.Profile, FunnelNames.Patcher),
                                                Path.Combine(prefixToUse, $"{patcherName}.txt"));
                                            finalWrite.File(
                                                formatter: new ExtraAttributeFormatter(FunnelNames.Profile),
                                                Path.Combine(prefixToUse, OverviewLogName),
                                                restrictedToMinimumLevel: LogEventLevel.Warning);
                                        }
                                        else if (profileName != null)
                                        {
                                            finalWrite.File(
                                                formatter: new ExtraAttributeFormatter(FunnelNames.Profile),
                                                Path.Combine(prefixToUse, OverviewLogName));
                                        }
                                    });
                            });
                    });
        }

    }
}
