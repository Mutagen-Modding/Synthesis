namespace Synthesis.Bethesda.Execution.Reporters.Classifications;

/// <summary>
/// Detects compression errors in captured output
/// </summary>
public class CompressionError : IErrorClassificationDetector
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

        // Look for Ionic.Zlib.ZlibException compression errors
        foreach (var line in allLines)
        {
            if (line.Contains("Ionic.Zlib.ZlibException", StringComparison.OrdinalIgnoreCase) ||
                (line.Contains("Bad state", StringComparison.OrdinalIgnoreCase) &&
                 line.Contains("compression method", StringComparison.OrdinalIgnoreCase)))
            {
                return new CompressionErrorClassification();
            }
        }

        return null;
    }
}

/// <summary>
/// Classification for compression errors
/// </summary>
public class CompressionErrorClassification : ErrorClassification
{
    public const string ErrorTypeString = "Compression Error";
    public const string SuggestionMessage = "A compression error occurred while reading a mod file. This typically happens when a mod file is corrupted or was compressed using an unsupported method. Try verifying your game files or re-downloading the affected mod.";

    public override string ErrorType => ErrorTypeString;
    public override string Message => SuggestionMessage;
    public override string? DiscussionLink => "https://github.com/Mutagen-Modding/Synthesis/discussions/544";
}
