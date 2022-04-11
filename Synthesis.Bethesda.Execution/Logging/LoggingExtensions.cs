using System;
using Noggog;
using Serilog;

namespace Synthesis.Bethesda.Execution.Logging;

public static class LoggingExtensions
{
    public static IDisposable Time(
        this ILogger logger,
        string description)
    {
        return new TimingMeasurement(logger, description);
    }

    public static void Error(this ILogger logger, ErrorResponse err, string extraMessage)
    {
        if (err.Exception == null)
        {
            logger.Error($"{extraMessage.Decorate(x => $"{x}: ")}{err.Reason}");
        }
        else
        {
            logger.Error(err.Exception, $"{extraMessage.Decorate(x => $"{x}: ")}");
        }
    }

    public static void Log(this ILogger logger, ErrorResponse err, string extraMessage)
    {
        if (err.Succeeded)
        {
            if (err.Exception == null)
            {
                logger.Information($"{extraMessage.Decorate(x => $"{x}: ")}{err.Reason}");
            }
            else
            {
                logger.Error(err.Exception, $"{extraMessage.Decorate(x => $"{x}: ")}");
            }
        }
        else
        {
            logger.Error(err, extraMessage);
        }
    }
}