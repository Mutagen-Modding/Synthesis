namespace Synthesis.Bethesda.Execution.Reporters.Classifications;

/// <summary>
/// Detects library version mismatch errors in captured output.
/// This typically occurs when an older version of Mutagen is paired with a newer version of Synthesis,
/// or vice versa, causing MissingMethodException or similar reflection errors at runtime.
/// </summary>
public class LibraryVersionMismatchError : IErrorClassificationDetector
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

        // Look for MissingMethodException, TypeLoadException, or FileLoadException
        // These typically indicate version mismatch between libraries
        foreach (var line in allLines)
        {
            if (line.Contains("MissingMethodException", StringComparison.OrdinalIgnoreCase))
            {
                return new LibraryVersionMismatchErrorClassification();
            }

            if (line.Contains("TypeLoadException", StringComparison.OrdinalIgnoreCase))
            {
                return new LibraryVersionMismatchErrorClassification();
            }

            if (line.Contains("FileLoadException", StringComparison.OrdinalIgnoreCase))
            {
                return new LibraryVersionMismatchErrorClassification();
            }
        }

        return null;
    }
}

/// <summary>
/// Classification for library version mismatch errors between Mutagen and Synthesis
/// </summary>
public class LibraryVersionMismatchErrorClassification : ErrorClassification
{
    public const string ErrorTypeString = "Library Version Mismatch";
    public const string SuggestionMessage =
        "A library version mismatch was detected. This typically occurs when an older version of Mutagen " +
        "is paired with a newer version of Synthesis, or vice versa. The libraries have incompatible APIs " +
        "that cause runtime errors. Please follow the recommended versioning setup to resolve this issue.";
    public const string VersioningDocsLink = "https://mutagen-modding.github.io/Synthesis/Versioning/#recommended-setup";

    public override string ErrorType => ErrorTypeString;
    public override string Message => SuggestionMessage;
    public override string? DiscussionLink => VersioningDocsLink;
}
