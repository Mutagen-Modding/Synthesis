using Synthesis.Bethesda.Execution.Exceptions;

namespace Synthesis.Bethesda.Execution.Reporters.Classifications;

/// <summary>
/// Detects Mo2BuildBlockedException and returns appropriate classification
/// </summary>
public class Mo2BuildBlockedExceptionDetector : IExceptionClassificationDetector
{
    public ErrorClassification? IsApplicable(Exception exception)
    {
        var current = exception;
        while (current != null)
        {
            if (current is Mo2BuildBlockedException)
            {
                return new Mo2BuildBlockedErrorClassification();
            }
            current = current.InnerException;
        }
        return null;
    }
}
