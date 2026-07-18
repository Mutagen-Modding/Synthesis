using System.Text.RegularExpressions;

namespace Synthesis.Bethesda.Execution.Reporters.Classifications;

/// <summary>
/// Detects when a patcher requires a .NET runtime version that is not installed.
/// This occurs when the patcher targets a different .NET version than what's available on the system.
/// </summary>
public class DotNetRuntimeMissingDetector : IErrorClassificationDetector
{
    private static readonly Regex FrameworkVersionPattern = new Regex(
        @"Framework:\s*'Microsoft\.NETCore\.App',\s*version\s*'([^']+)'",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public ErrorClassification? IsApplicable(
        IReadOnlyList<string>? capturedOutput,
        IReadOnlyList<string>? capturedErrors)
    {
        var allLines = new List<string>();
        if (capturedOutput != null) allLines.AddRange(capturedOutput);
        if (capturedErrors != null) allLines.AddRange(capturedErrors);

        if (allLines.Count == 0) return null;

        bool detected = false;
        string? requiredVersion = null;

        foreach (var line in allLines)
        {
            if (line.Contains("You must install or update .NET to run this application", StringComparison.OrdinalIgnoreCase))
            {
                detected = true;
            }

            var match = FrameworkVersionPattern.Match(line);
            if (match.Success)
            {
                requiredVersion = match.Groups[1].Value;
            }
        }

        if (!detected) return null;

        return new DotNetRuntimeMissingErrorClassification(requiredVersion);
    }
}

/// <summary>
/// Classification for when a patcher requires a .NET runtime version that is not installed.
/// </summary>
public class DotNetRuntimeMissingErrorClassification : ErrorClassification
{
    public const string ErrorTypeString = ".NET Runtime Missing";
    public const string SuggestionMessage =
        "The patcher requires a .NET runtime version that is not installed on your system.\n\n" +
        "Note that the .NET Runtime is not the same as the .NET SDK.  " +
        "Even if you have a newer SDK installed, patchers targeting older .NET versions still need " +
        "the matching runtime to be installed separately.";
    public const string DocsLink = "https://mutagen-modding.github.io/Synthesis/Installation/#runtime-is-not-the-sdk";

    /// <summary>
    /// The .NET runtime version that the patcher requires (e.g. "9.0.0"), or null if not parsed.
    /// </summary>
    public string? RequiredVersion { get; }

    public DotNetRuntimeMissingErrorClassification(string? requiredVersion = null)
    {
        RequiredVersion = requiredVersion;
    }

    public override string ErrorType => ErrorTypeString;
    public override string Message => SuggestionMessage;
    public override string? DiscussionLink => DocsLink;
}
