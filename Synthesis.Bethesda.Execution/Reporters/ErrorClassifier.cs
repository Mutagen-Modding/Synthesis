namespace Synthesis.Bethesda.Execution.Reporters;

/// <summary>
/// Default implementation of IErrorClassifier that checks for known error patterns
/// </summary>
public class ErrorClassifier : IErrorClassifier
{
    public ErrorClassification? Classify(
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

        // Check for TooManyMasters error
        // This error appears when the patcher tries to write a plugin with too many masters
        if (IsTooManyMastersError(allLines))
        {
            return new TooManyMastersError();
        }

        // Check for ReferencedModMissing error
        // This error appears when a mod referenced during load order sorting is not present
        if (IsReferencedModMissingError(allLines))
        {
            return new ReferencedModMissingError();
        }

        // Add more error classifications here as needed

        return null;
    }

    private static bool IsTooManyMastersError(IReadOnlyList<string> lines)
    {
        // Look for the TooManyMastersException message in the output
        // The exception message typically contains "TooManyMastersException"
        foreach (var line in lines)
        {
            if (line.Contains("TooManyMastersException", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsReferencedModMissingError(IReadOnlyList<string> lines)
    {
        // Look for the "Referenced mod was not present on the load order being sorted against" message
        foreach (var line in lines)
        {
            if (line.Contains("Referenced mod was not present on the load order being sorted against", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
