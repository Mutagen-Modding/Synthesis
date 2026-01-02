namespace Synthesis.Bethesda.Execution.Reporters.Classifications;

/// <summary>
/// Default implementation of IErrorClassifier that delegates to registered detectors
/// </summary>
public class ErrorClassifier : IErrorClassifier
{
    private readonly IErrorClassificationDetector[] _detectors;

    public ErrorClassifier(IErrorClassificationDetector[] detectors)
    {
        _detectors = detectors;
    }

    public ErrorClassification? Classify(
        IReadOnlyList<string>? capturedOutput,
        IReadOnlyList<string>? capturedErrors)
    {
        // Loop through each detector and let them decide if they're applicable
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
