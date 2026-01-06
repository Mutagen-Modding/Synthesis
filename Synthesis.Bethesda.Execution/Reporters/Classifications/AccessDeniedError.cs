using System.Text.RegularExpressions;

namespace Synthesis.Bethesda.Execution.Reporters.Classifications;

/// <summary>
/// Detects file access denied errors in captured output
/// </summary>
public class AccessDeniedError : IErrorClassificationDetector
{
    private static readonly Regex AccessDeniedPattern = new Regex(
        @"System\.IO\.IOException:\s+The process cannot access the file\s+'([^']+)'\s+because it is being used by another process",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

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

        // Look for access denied errors
        foreach (var line in allLines)
        {
            var match = AccessDeniedPattern.Match(line);
            if (match.Success)
            {
                var filePath = match.Groups[1].Value;
                return new AccessDeniedErrorClassification(filePath);
            }
        }

        return null;
    }
}

/// <summary>
/// Classification for file access denied errors
/// </summary>
public class AccessDeniedErrorClassification : ErrorClassification
{
    public string FilePath { get; }

    public AccessDeniedErrorClassification(string filePath)
    {
        FilePath = filePath;
    }

    public override string ErrorType => "File Access Denied";
    public override string Message => "The process cannot access a file because it is being used by another process.  This is typically a red herring due to other causes.\n\n" +
                                      "Antivirus programs can cause interference:  https://github.com/Mutagen-Modding/Synthesis/discussions/203\n\n" +
                                      "Mod Organizer also does not play well with the latest SDKs.   Try downgrading to SDK 9 if you are on 10.";
    public override string? DiscussionLink => "https://github.com/Mutagen-Modding/Synthesis/discussions/419";
}
