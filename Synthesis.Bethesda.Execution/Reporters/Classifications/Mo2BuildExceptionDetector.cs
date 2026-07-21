using Synthesis.Bethesda.Execution.Exceptions;

namespace Synthesis.Bethesda.Execution.Reporters.Classifications;

/// <summary>
/// Detects Mo2BuildException and returns the MO2 build error classification.
/// This only fires for genuine build failures inside MO2, not for unrelated access denied errors.
/// </summary>
public class Mo2BuildExceptionDetector : IExceptionClassificationDetector
{
    public ErrorClassification? IsApplicable(Exception exception)
    {
        var current = exception;
        while (current != null)
        {
            if (current is Mo2BuildException mo2)
            {
                return new RanBuildInMo2ErrorClassification(mo2.FilePath);
            }
            current = current.InnerException;
        }
        return null;
    }
}
