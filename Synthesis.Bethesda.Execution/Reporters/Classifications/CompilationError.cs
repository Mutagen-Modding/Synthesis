using Synthesis.Bethesda.Execution.Exceptions;

namespace Synthesis.Bethesda.Execution.Reporters.Classifications;

/// <summary>
/// Detects SynthesisBuildFailure exceptions indicating a compilation failure.
/// This typically occurs when an older patcher targets newer libraries with incompatible APIs,
/// causing the dotnet build to fail.
/// </summary>
public class CompilationExceptionDetector : IExceptionClassificationDetector
{
    public ErrorClassification? IsApplicable(Exception exception)
    {
        var current = exception;
        while (current != null)
        {
            if (current is SynthesisBuildFailure buildFailure
                && !CscCrashErrorClassification.IsCscCrash(buildFailure.Message))
            {
                return new CompilationErrorClassification(buildFailure.Message);
            }
            current = current.InnerException;
        }
        return null;
    }
}

/// <summary>
/// Classification for compilation errors where a patcher fails to build,
/// typically due to API changes when targeting newer library versions.
/// </summary>
public class CompilationErrorClassification : ErrorClassification
{
    public const string ErrorTypeString = "Compilation Error";
    public const string SuggestionMessage =
        "The patcher failed to compile. This typically occurs when the patcher targets a newer version of " +
        "the Mutagen or Synthesis libraries whose API has changed in ways that are incompatible with the " +
        "patcher's code. Consider using \"Set to Match\" to target a version that the patcher " +
        "was built against, or opening an issue on the patcher's source code to nudge them to update.";
    public const string VersioningDocsLink = "https://mutagen-modding.github.io/Synthesis/Versioning/#recommended-setup";

    public string CompilationText { get; }

    public CompilationErrorClassification(string compilationText)
    {
        CompilationText = compilationText;
    }

    public override string ErrorType => ErrorTypeString;
    public override string Message => SuggestionMessage;
    public override string? DiscussionLink => VersioningDocsLink;
}
