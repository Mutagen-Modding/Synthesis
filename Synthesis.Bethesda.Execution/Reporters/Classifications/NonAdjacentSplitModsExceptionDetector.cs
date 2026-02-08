using Mutagen.Bethesda.Plugins;
using Synthesis.Bethesda.Execution.Exceptions;

namespace Synthesis.Bethesda.Execution.Reporters.Classifications;

/// <summary>
/// Detects NonAdjacentSplitModsException and returns appropriate classification
/// </summary>
public class NonAdjacentSplitModsExceptionDetector : IExceptionClassificationDetector
{
    public ErrorClassification? IsApplicable(Exception exception)
    {
        var current = exception;
        while (current != null)
        {
            if (current is NonAdjacentSplitModsException splitModsException)
            {
                return new NonAdjacentSplitModsErrorClassification(
                    splitModsException.BaseModKey,
                    splitModsException.SplitModKeys,
                    Array.Empty<ModKey>());
            }
            current = current.InnerException;
        }
        return null;
    }
}
