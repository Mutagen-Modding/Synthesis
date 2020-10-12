using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace Synthesis.Bethesda.GUI
{
    public static class Log
    {
        public static readonly Logger Logger;
        public static readonly string LogPath;
        public static readonly DateTime StartTime;

        static Log()
        {
            StartTime = DateTime.Now;
            LogPath = "logs/log-.txt";
            Logger = GetLoggerConfig()
                .WriteToFile()
                .CreateLogger();
        }

        public static LoggerConfiguration GetLoggerConfig()
        {
            return new LoggerConfiguration()
                .MinimumLevel.Debug();
        }

        public static LoggerConfiguration WriteToFile(this LoggerConfiguration conf, params string[] extraIdentifiers)
        {
            return conf
                .WriteTo.File(
                    formatter: new ExtraAttributeFormatter(),
                    path: LogPath,
                    rollingInterval: RollingInterval.Day);
        }

    }
}
