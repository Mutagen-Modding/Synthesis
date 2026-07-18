namespace Synthesis.Bethesda.Execution.Reporters.Classifications;

/// <summary>
/// Interface for detecting specific error classifications from captured output
/// </summary>
public interface IErrorClassificationDetector
{
    /// <summary>
    /// Checks if this detector is applicable to the given output and returns the classification if so
    /// </summary>
    /// <param name="capturedOutput">Output lines from the process</param>
    /// <param name="capturedErrors">Error lines from the process</param>
    /// <returns>ErrorClassification if applicable, null otherwise</returns>
    ErrorClassification? IsApplicable(
        IReadOnlyList<string>? capturedOutput,
        IReadOnlyList<string>? capturedErrors);
}
