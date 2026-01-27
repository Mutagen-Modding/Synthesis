using Synthesis.Bethesda.Execution.DotNet.Builder;

namespace Synthesis.Bethesda.Execution.Reporters.Classifications;

/// <summary>
/// Detects when a build was blocked due to the BlockBuildingWithinMo2 setting being enabled while running in MO2
/// </summary>
public class BlockedByMo2SettingError : IErrorClassificationDetector
{
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

        if (allLines.Count == 0)
        {
            return null;
        }

        // Look for the MO2 blocked marker
        foreach (var line in allLines)
        {
            if (line.Contains(Build.Mo2BlockedMarker))
            {
                return new RanBuildInMo2ErrorClassification(string.Empty);
            }
        }

        return null;
    }
}
