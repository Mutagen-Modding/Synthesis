using Synthesis.Bethesda.Execution.Utility;

namespace Synthesis.Bethesda.Execution.Reporters.Classifications;

public class Mo2MsBuildServerErrorDetector : IErrorClassificationDetector, IExceptionClassificationDetector
{
    private readonly IMo2EnvironmentDetector _mo2Detector;

    public Mo2MsBuildServerErrorDetector(IMo2EnvironmentDetector mo2Detector)
    {
        _mo2Detector = mo2Detector;
    }

    public static bool IsMsBuildServerFailure(string line)
    {
        return line.Contains("MSBuild server unavailable", StringComparison.OrdinalIgnoreCase);
    }

    public ErrorClassification? IsApplicable(
        IReadOnlyList<string>? capturedOutput,
        IReadOnlyList<string>? capturedErrors)
    {
        if (!_mo2Detector.IsRunningInsideMo2()) return null;
        var lines = (capturedOutput ?? []).Concat(capturedErrors ?? []);
        return lines.Any(IsMsBuildServerFailure)
            ? new RanBuildInMo2ErrorClassification()
            : null;
    }

    public ErrorClassification? IsApplicable(Exception exception)
    {
        if (!_mo2Detector.IsRunningInsideMo2()) return null;
        for (var current = exception; current != null; current = current.InnerException)
        {
            if (IsMsBuildServerFailure(current.Message))
            {
                return new RanBuildInMo2ErrorClassification();
            }
        }
        return null;
    }
}
