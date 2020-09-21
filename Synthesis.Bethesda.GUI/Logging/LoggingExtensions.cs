using Serilog;
using Serilog.Events;
using Synthesis.Bethesda.GUI.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Synthesis.Bethesda.GUI
{
    public static class LoggingExtensions
    {
        public static IDisposable Time(
            this ILogger logger,
            string description)
        {
            return new TimingMeasurement(logger, description);
        }
    }
}
