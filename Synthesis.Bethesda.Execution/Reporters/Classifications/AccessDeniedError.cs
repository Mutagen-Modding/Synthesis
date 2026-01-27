using System.Text.RegularExpressions;
using Synthesis.Bethesda.Execution.Utility;

namespace Synthesis.Bethesda.Execution.Reporters.Classifications;

/// <summary>
/// Detects file access denied errors in captured output
/// </summary>
public class AccessDeniedError : IErrorClassificationDetector
{
    private readonly IMo2EnvironmentDetector _mo2Detector;

    private static readonly Regex AccessDeniedPattern = new Regex(
        @"System\.IO\.IOException:\s+The process cannot access the file\s+'([^']+)'\s+because it is being used by another process",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public AccessDeniedError(IMo2EnvironmentDetector mo2Detector)
    {
        _mo2Detector = mo2Detector;
    }

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

                // If running inside MO2, return the MO2-specific classification
                if (_mo2Detector.IsRunningInsideMo2())
                {
                    return new RanBuildInMo2ErrorClassification(filePath);
                }

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
    public override string Message => "The process cannot access a file because it is being used by another process. This is typically a red herring due to other causes.\n\n" +
                                      "Antivirus programs can cause interference: https://github.com/Mutagen-Modding/Synthesis/discussions/203";
    public override string? DiscussionLink => "https://github.com/Mutagen-Modding/Synthesis/discussions/419";
}
