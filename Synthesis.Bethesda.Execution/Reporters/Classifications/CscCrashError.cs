using Synthesis.Bethesda.Execution.Exceptions;
using Synthesis.Bethesda.Execution.Utility;

namespace Synthesis.Bethesda.Execution.Reporters.Classifications;

/// <summary>
/// Detects csc.exe crashes (exit code -1073741819 / 0xC0000005 access violation).
/// This specific build failure can be caused by MO2's VFS interfering with the compiler
/// or by antivirus software blocking csc.exe.
/// Must be checked before the generic CompilationExceptionDetector.
/// </summary>
public class CscCrashExceptionDetector : IExceptionClassificationDetector, IErrorClassificationDetector
{
    private readonly IMo2EnvironmentDetector _mo2Detector;

    public CscCrashExceptionDetector(IMo2EnvironmentDetector mo2Detector)
    {
        _mo2Detector = mo2Detector;
    }

    public ErrorClassification? IsApplicable(Exception exception)
    {
        var current = exception;
        while (current != null)
        {
            if (current is SynthesisBuildFailure buildFailure
                && CscCrashErrorClassification.IsCscCrash(buildFailure.Message))
            {
                return new CscCrashErrorClassification(
                    buildFailure.Message,
                    _mo2Detector.IsRunningInsideMo2());
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
            if (CscCrashErrorClassification.IsCscCrash(line))
            {
                return new CscCrashErrorClassification(
                    string.Join(Environment.NewLine, allLines),
                    _mo2Detector.IsRunningInsideMo2());
            }
        }

        return null;
    }
}

/// <summary>
/// Classification for csc.exe crash errors (MSB6006 with exit code -1073741819).
/// Provides guidance about both MO2 VFS and antivirus interference as possible causes.
/// </summary>
public class CscCrashErrorClassification : ErrorClassification
{
    public const string ErrorTypeString = "Compiler Crash (csc.exe)";
    public const string SuggestionMessage =
        "The C# compiler (csc.exe) crashed with an access violation during the build. " +
        "This is typically caused by external software interfering with the compilation process, " +
        "such as antivirus or Mod Organizer 2's virtual file system.";
    public const string FaqLink = "https://github.com/Mutagen-Modding/Synthesis/discussions/203";

    public string CompilationText { get; }
    public bool IsRunningInsideMo2 { get; }

    public CscCrashErrorClassification(string compilationText, bool isRunningInsideMo2)
    {
        CompilationText = compilationText;
        IsRunningInsideMo2 = isRunningInsideMo2;
    }

    public override string ErrorType => ErrorTypeString;
    public override string Message => IsRunningInsideMo2
        ? RanBuildInMo2ErrorClassification.SuggestionMessage
        : SuggestionMessage;
    public override string? DiscussionLink => FaqLink;

    /// <summary>
    /// Checks whether a build failure message contains the csc.exe crash pattern.
    /// Matches: error MSB6006: "csc.exe" exited with code -1073741819
    /// </summary>
    public static bool IsCscCrash(string message)
    {
        return message.Contains("csc.exe", StringComparison.OrdinalIgnoreCase)
               && (message.Contains("-1073741819", StringComparison.Ordinal)
                   || message.Contains("0xC0000005", StringComparison.OrdinalIgnoreCase));
    }
}
