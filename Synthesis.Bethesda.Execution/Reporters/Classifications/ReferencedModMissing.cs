namespace Synthesis.Bethesda.Execution.Reporters.Classifications;

/// <summary>
/// Detects ReferencedModMissing errors in captured output
/// </summary>
public class ReferencedModMissing : IErrorClassificationDetector
{
    public ErrorClassification? IsApplicable(
        IReadOnlyList<string>? capturedOutput,
        IReadOnlyList<string>? capturedErrors)
    {
        // Combine all captured text for analysis
        var allLines = new List<string>();
        if (capturedOutput != null)
        {
            allLines.AddRange(capturedOutput);
        }
        if (capturedErrors != null)
        {
            allLines.AddRange(capturedErrors);
        }

        if (allLines.Count == 0)
        {
            return null;
        }

        // Look for the "Referenced mod was not present on the load order being sorted against" message
        foreach (var line in allLines)
        {
            if (line.Contains("Referenced mod was not present on the load order being sorted against", StringComparison.OrdinalIgnoreCase))
            {
                return new ReferencedModMissingError();
            }
        }

        return null;
    }
}

/// <summary>
/// Classification for ReferencedModMissing errors
/// </summary>
public class ReferencedModMissingError : ErrorClassification
{
    public const string SuggestionMessage = "A referenced mod was not present on the load order being sorted against. This typically happens when a patcher references a mod that isn't in your current load order. Check your load order and ensure all required mods are present and enabled.";

    public override string ErrorType => "Referenced Mod Missing";
    public override string Message => SuggestionMessage;
}
