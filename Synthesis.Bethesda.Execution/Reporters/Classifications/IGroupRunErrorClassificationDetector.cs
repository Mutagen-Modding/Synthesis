using Mutagen.Bethesda.Plugins.Order;

namespace Synthesis.Bethesda.Execution.Reporters.Classifications;

/// <summary>
/// Interface for detecting specific error classifications from captured output
/// with additional context from the group run (e.g., load order)
/// </summary>
public interface IGroupRunErrorClassificationDetector
{
    /// <summary>
    /// Checks if this detector is applicable to the given output and returns the classification if so
    /// </summary>
    /// <param name="capturedOutput">Output lines from the process</param>
    /// <param name="capturedErrors">Error lines from the process</param>
    /// <param name="loadOrder">The load order for the current group run</param>
    /// <returns>ErrorClassification if applicable, null otherwise</returns>
    ErrorClassification? IsApplicable(
        IReadOnlyList<string>? capturedOutput,
        IReadOnlyList<string>? capturedErrors,
        IList<ILoadOrderListingGetter> loadOrder);
}
