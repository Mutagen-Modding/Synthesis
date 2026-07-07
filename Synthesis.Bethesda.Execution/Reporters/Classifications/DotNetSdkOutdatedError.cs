using System.Text.RegularExpressions;
using Synthesis.Bethesda.Execution.Exceptions;

namespace Synthesis.Bethesda.Execution.Reporters.Classifications;

/// <summary>
/// Detects when the installed .NET SDK is too old to build a patcher that targets a newer .NET version.
/// Emitted by MSBuild as NETSDK1045: "The current .NET SDK does not support targeting .NET X.Y".
/// Must be checked before the generic CompilationExceptionDetector.
/// </summary>
public class DotNetSdkOutdatedDetector : IExceptionClassificationDetector, IErrorClassificationDetector
{
    public ErrorClassification? IsApplicable(Exception exception)
    {
        var current = exception;
        while (current != null)
        {
            if (current is SynthesisBuildFailure buildFailure
                && DotNetSdkOutdatedErrorClassification.IsSdkOutdated(buildFailure.Message))
            {
                return new DotNetSdkOutdatedErrorClassification(
                    DotNetSdkOutdatedErrorClassification.ExtractTargetVersion(buildFailure.Message));
            }
            current = current.InnerException;
        }
        return null;
    }

    public ErrorClassification? IsApplicable(
        IReadOnlyList<string>? capturedOutput,
        IReadOnlyList<string>? capturedErrors)
    {
        var allLines = new List<string>();
        if (capturedOutput != null) allLines.AddRange(capturedOutput);
        if (capturedErrors != null) allLines.AddRange(capturedErrors);

        foreach (var line in allLines)
        {
            if (DotNetSdkOutdatedErrorClassification.IsSdkOutdated(line))
            {
                return new DotNetSdkOutdatedErrorClassification(
                    DotNetSdkOutdatedErrorClassification.ExtractTargetVersion(line));
            }
        }

        return null;
    }
}

/// <summary>
/// Classification for when the installed .NET SDK is too old to build a patcher targeting a newer .NET version.
/// Recommends installing the latest SDK.
/// </summary>
public class DotNetSdkOutdatedErrorClassification : ErrorClassification
{
    public const string Marker = "The current .NET SDK does not support targeting";
    public const string ErrorTypeString = ".NET SDK Out of Date";
    public const string DownloadLink = "https://dotnet.microsoft.com/download";

    private static readonly Regex TargetVersionPattern = new Regex(
        @"does not support targeting \.NET(?: Core)?\s+([0-9]+(?:\.[0-9]+)*)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// The .NET version the patcher targets that the installed SDK cannot build (e.g. "9.0"), or null if not parsed.
    /// </summary>
    public string? TargetVersion { get; }

    public DotNetSdkOutdatedErrorClassification(string? targetVersion = null)
    {
        TargetVersion = targetVersion;
    }

    public override string ErrorType => ErrorTypeString;

    public override string Message =>
        (TargetVersion != null
            ? $"This patcher targets .NET {TargetVersion}, but your installed .NET SDK is too old to build it.\n\n"
            : "This patcher targets a newer version of .NET than your installed .NET SDK is able to build.\n\n")
        + "Install the latest .NET SDK and try running again.";

    public override string? DiscussionLink => DownloadLink;

    /// <summary>
    /// Checks whether a build failure message contains the outdated-SDK pattern (NETSDK1045).
    /// </summary>
    public static bool IsSdkOutdated(string message)
    {
        return message.Contains(Marker, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Extracts the targeted .NET version from an outdated-SDK message (e.g. "9.0"), or null if not present.
    /// </summary>
    public static string? ExtractTargetVersion(string message)
    {
        var match = TargetVersionPattern.Match(message);
        return match.Success ? match.Groups[1].Value : null;
    }
}
