namespace Synthesis.Bethesda.Execution.Reporters;

/// <summary>
/// Service that analyzes patcher output to detect known error patterns
/// </summary>
public interface IErrorClassifier
{
    /// <summary>
    /// Attempts to classify an error based on captured patcher output
    /// </summary>
    /// <param name="capturedOutput">Captured stdout from the failed patcher</param>
    /// <param name="capturedErrors">Captured stderr from the failed patcher</param>
    /// <returns>An error classification if a known pattern is detected, otherwise null</returns>
    ErrorClassification? Classify(
        IReadOnlyList<string>? capturedOutput,
        IReadOnlyList<string>? capturedErrors);
}
