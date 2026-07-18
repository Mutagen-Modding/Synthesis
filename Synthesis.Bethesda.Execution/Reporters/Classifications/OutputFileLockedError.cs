using System.Text.RegularExpressions;

namespace Synthesis.Bethesda.Execution.Reporters.Classifications;

/// <summary>
/// Detects IOException when a file is locked by another process
/// </summary>
public class OutputFileLockedExceptionDetector : IExceptionClassificationDetector
{
    private static readonly Regex FilePathPattern = new(
        @"The process cannot access the file '([^']+)' because it is being used by another process",
        RegexOptions.Compiled);

    public ErrorClassification? IsApplicable(Exception exception)
    {
        var current = exception;
        while (current != null)
        {
            if (current is IOException ioEx && ioEx.Message.Contains("being used by another process"))
            {
                var match = FilePathPattern.Match(ioEx.Message);
                var filePath = match.Success ? match.Groups[1].Value : string.Empty;
                return new OutputFileLockedErrorClassification(filePath);
            }
            current = current.InnerException;
        }
        return null;
    }
}

/// <summary>
/// Classification for when the output patch file cannot be exported because it is locked by another process
/// </summary>
public class OutputFileLockedErrorClassification : ErrorClassification
{
    public const string ErrorTypeString = "Output File Locked";

    public string FilePath { get; }

    public OutputFileLockedErrorClassification(string filePath)
    {
        FilePath = filePath;
    }

    public override string ErrorType => ErrorTypeString;

    public override string Message =>
        "The output patch file could not be exported because it is locked by another process.\n\n" +
        "Please close any programs that may have this file open, such as:\n" +
        "  - xEdit (SSEEdit, FO4Edit, etc.)\n" +
        "  - Creation Kit\n" +
        "  - Other mod tools or file managers\n\n" +
        "Then try running Synthesis again.";

    public override void LogCliDetails(Action<string> log)
    {
        if (!string.IsNullOrEmpty(FilePath))
        {
            log($"Locked file: {FilePath}");
        }
    }
}
