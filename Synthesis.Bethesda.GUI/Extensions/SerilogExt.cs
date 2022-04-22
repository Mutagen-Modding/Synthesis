using Serilog;

namespace Synthesis.Bethesda.GUI;

public static class SerilogExt
{
    public static ILogger ForContextIfNotNull(this ILogger logger, string propertyName, object? value, bool destructureObjects = false)
    {
        if (value == null) return logger;
        return logger.ForContext(propertyName, value, destructureObjects);
    }
}