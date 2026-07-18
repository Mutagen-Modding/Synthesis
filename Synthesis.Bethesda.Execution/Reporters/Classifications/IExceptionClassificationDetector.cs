namespace Synthesis.Bethesda.Execution.Reporters.Classifications;

/// <summary>
/// Interface for detecting specific error classifications from exceptions
/// </summary>
public interface IExceptionClassificationDetector
{
    /// <summary>
    /// Checks if this detector is applicable to the given exception and returns the classification if so
    /// </summary>
    /// <param name="exception">The exception to check</param>
    /// <returns>ErrorClassification if applicable, null otherwise</returns>
    ErrorClassification? IsApplicable(Exception exception);
}
