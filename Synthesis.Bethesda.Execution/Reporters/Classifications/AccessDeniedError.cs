using System.ComponentModel;
using Microsoft.Extensions.Logging;

namespace Synthesis.Bethesda.Execution.Reporters.Classifications;

/// <summary>
/// Detects file access denied errors in captured output and exceptions.
/// MO2-caused build failures are handled separately via <see cref="Mo2BuildExceptionDetector"/>,
/// so this always reports a plain access denied error regardless of the MO2 environment.
/// </summary>
public class AccessDeniedError : IErrorClassificationDetector, IExceptionClassificationDetector
{
    private const int ERROR_ACCESS_DENIED = 5;
    private readonly ILogger<AccessDeniedError> _logger;

    public AccessDeniedError(
        ILogger<AccessDeniedError> logger)
    {
        _logger = logger;
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

        if (AccessDeniedDetection.TryFind(allLines, out var filePath))
        {
            return new AccessDeniedErrorClassification(filePath);
        }

        return null;
    }

    public ErrorClassification? IsApplicable(Exception exception)
    {
        // Check the exception chain for Win32Exception with ACCESS_DENIED error code
        var current = exception;
        while (current != null)
        {
            if (current is Win32Exception win32Ex && win32Ex.NativeErrorCode == ERROR_ACCESS_DENIED)
            {
                _logger.LogInformation("Detected Win32Exception ACCESS_DENIED");
                return new AccessDeniedErrorClassification(string.Empty);
            }
            current = current.InnerException;
        }

        return null;
    }
}

/// <summary>
/// Classification for file access denied errors
/// </summary>
public class AccessDeniedErrorClassification : ErrorClassification
{
    public const string ErrorTypeString = "File Access Denied";

    public string FilePath { get; }

    public AccessDeniedErrorClassification(string filePath)
    {
        FilePath = filePath;
    }

    public override string ErrorType => ErrorTypeString;
    public override string Message => "The process cannot access a file because it is being used by another process. This is typically a red herring due to other causes.";
    public override string? DiscussionLink => "https://github.com/Mutagen-Modding/Synthesis/discussions/564";
}
