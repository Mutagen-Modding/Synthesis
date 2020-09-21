using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Synthesis.Bethesda.GUI.Logging
{
    public class TimingMeasurement : IDisposable
    {
        public readonly ILogger Logger;
        public readonly string Message;
        private readonly Stopwatch _sw;

        public TimingMeasurement(ILogger logger, string message)
        {
            Message = message;
            Logger = logger;
            Logger.Information("Starting {Message}", Message);
            _sw = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            _sw.Stop();
            Logger.Information("Completed {Message} in {Elapsed} ({ElapsedMs}ms)", Message, _sw.Elapsed, _sw.ElapsedMilliseconds);
        }
    }
}
