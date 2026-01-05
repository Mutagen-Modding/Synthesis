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

    public ErrorClassifier(
        IErrorClassificationDetector[] detectors,
        IGroupRunErrorClassificationDetector[] groupRunDetectors)
    {
        _detectors = detectors;
        _groupRunDetectors = groupRunDetectors;
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
}
