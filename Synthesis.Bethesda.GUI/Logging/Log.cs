using Serilog;
using System;

namespace Synthesis.Bethesda.GUI
{
    public static class Log
    {
        public static readonly ILogger Logger;
        public static readonly DateTime StartTime;

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
            return conf
                .WriteTo.Map(
                    "PatcherName",
                    default(string?),
                    (key, wt) =>
                    {
                        wt.File(
                            formatter: new ExtraAttributeFormatter(),
                            $"logs/{(key ?? "Main")}.txt",
                            rollingInterval: RollingInterval.Day);
                    });
        }

    }
}
