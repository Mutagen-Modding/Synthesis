using Mutagen.Bethesda.Plugins.Order;
using Synthesis.Bethesda.Execution.Reporters;

namespace Synthesis.Bethesda.Execution.Reporters.Classifications;

/// <summary>
/// Default implementation of IErrorClassifier that delegates to registered detectors
/// </summary>
public class ErrorClassifier : IErrorClassifier
{
    private readonly IErrorClassificationDetector[] _detectors;
    private readonly IGroupRunErrorClassificationDetector[] _groupRunDetectors;
    private readonly IExceptionClassificationDetector[] _exceptionDetectors;

    public ErrorClassifier(
        IErrorClassificationDetector[] detectors,
        IGroupRunErrorClassificationDetector[] groupRunDetectors,
        IExceptionClassificationDetector[] exceptionDetectors)
    {
        _detectors = detectors;
        _groupRunDetectors = groupRunDetectors;
        _exceptionDetectors = exceptionDetectors;
    }

    public ErrorClassification? Classify(
        IReadOnlyList<string>? capturedOutput,
        IReadOnlyList<string>? capturedErrors,
        IList<ILoadOrderListingGetter>? loadOrder = null)
    {
        // First try group-run detectors if we have load order context
        if (loadOrder != null)
        {
            foreach (var detector in _groupRunDetectors)
            {
                var classification = detector.IsApplicable(capturedOutput, capturedErrors, loadOrder);
                if (classification != null)
                {
                    return classification;
                }
            }
        }

        // Then fall back to simple detectors
        foreach (var detector in _detectors)
        {
            var classification = detector.IsApplicable(capturedOutput, capturedErrors);
            if (classification != null)
            {
                return classification;
            }
        }

        return null;
    }

    public ErrorClassification? Classify(Exception exception)
    {
        // First try exception-type detectors
        foreach (var detector in _exceptionDetectors)
        {
            var classification = detector.IsApplicable(exception);
            if (classification != null)
            {
                return classification;
            }
        }

        // Fall back to string-based classification from exception messages
        var exceptionLines = new List<string>();
        var current = exception;
        while (current != null)
        {
            if (!string.IsNullOrWhiteSpace(current.Message))
            {
                exceptionLines.Add(current.Message);
            }
            current = current.InnerException;
        }

        return Classify(exceptionLines, null);
    }
}
