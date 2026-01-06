namespace Synthesis.Bethesda.Execution.Reporters.Classifications;

/// <summary>
/// Detects TooManyMasters errors in captured output
/// </summary>
public class TooManyMasters : IErrorClassificationDetector
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

        // Look for the TooManyMastersException message in the output
        // The exception message typically contains "TooManyMastersException"
        foreach (var line in allLines)
        {
            if (line.Contains("TooManyMastersException", StringComparison.OrdinalIgnoreCase))
            {
                return new TooManyMastersError();
            }
        }

        return null;
    }
}

/// <summary>
/// Classification for TooManyMasters errors
/// </summary>
public class TooManyMastersError : ErrorClassification
{
    public override string ErrorType => "Too Many Masters";
    public override string Message => "The output plugin has too many references to other mods.";
    public override string? DiscussionLink => "https://github.com/Mutagen-Modding/Synthesis/discussions/300";
}
