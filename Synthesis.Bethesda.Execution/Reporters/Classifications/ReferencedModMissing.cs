using System.Linq;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Order;

namespace Synthesis.Bethesda.Execution.Reporters.Classifications;

/// <summary>
/// Detects ReferencedModMissing errors in captured output
/// </summary>
public class ReferencedModMissing : IGroupRunErrorClassificationDetector
{
    public ErrorClassification? IsApplicable(
        IReadOnlyList<string>? capturedOutput,
        IReadOnlyList<string>? capturedErrors,
        IList<ILoadOrderListingGetter> loadOrder)
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

        // Look for the "Referenced mod was not present on the load order being sorted against" message
        foreach (var line in allLines)
        {
            if (line.Contains("referenced mod was not present on the load order being sorted against", StringComparison.OrdinalIgnoreCase))
            {
                // Try to extract the missing ModKey and FormKey from the error message
                // Format: "A referenced mod was not present on the load order being sorted against: ModKey.esp.  This mod was referenced by MajorRecord: 000000:Synthesis.esp    at ..."
                ModKey missingMod = ModKey.FromNameAndExtension("Unknown.esp");
                FormKey referencedBy = FormKey.Factory("000000:Unknown.esp");

                // Extract ModKey - look for text between "sorted against: " and ". "
                var sortedAgainstIndex = line.IndexOf("sorted against: ", StringComparison.OrdinalIgnoreCase);
                if (sortedAgainstIndex >= 0)
                {
                    var startIndex = sortedAgainstIndex + "sorted against: ".Length;
                    var endIndex = line.IndexOf(". ", startIndex);
                    if (endIndex > startIndex)
                    {
                        var modKeyStr = line.Substring(startIndex, endIndex - startIndex).Trim();
                        if (ModKey.TryFromNameAndExtension(modKeyStr, out var parsedModKey))
                        {
                            missingMod = parsedModKey;
                        }
                    }
                }

                // Extract FormKey - look for text after "MajorRecord: " and parse the first token
                var majorRecordIndex = line.IndexOf("MajorRecord: ", StringComparison.OrdinalIgnoreCase);
                if (majorRecordIndex >= 0)
                {
                    var startIndex = majorRecordIndex + "MajorRecord: ".Length;
                    var extracted = line.Substring(startIndex).Trim();
                    if (!string.IsNullOrWhiteSpace(extracted))
                    {
                        // Extract just the first token (FormKey format: "FORMID:ModName.esp")
                        var firstToken = extracted.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                        if (!string.IsNullOrWhiteSpace(firstToken) && FormKey.TryFactory(firstToken, out var parsedFormKey))
                        {
                            referencedBy = parsedFormKey;
                        }
                    }
                }

                return new ReferencedModMissingError(loadOrder, missingMod, referencedBy);
            }
        }

        return null;
    }
}

/// <summary>
/// Classification for ReferencedModMissing errors
/// </summary>
public class ReferencedModMissingError : ErrorClassification
{
    public const string SuggestionMessage = "A referenced mod was not present on the load order being sorted against. This typically happens when a patcher references a mod that isn't in your current load order. Check your load order and ensure all required mods are present and enabled.";

    public override string ErrorType => "Referenced Mod Missing";
    public override string Message => SuggestionMessage;
    public override string? DiscussionLink => "https://github.com/Mutagen-Modding/Synthesis/discussions/382";

    /// <summary>
    /// The load order at the time of the error, for user reference
    /// </summary>
    public IList<ILoadOrderListingGetter> LoadOrder { get; }

    /// <summary>
    /// The ModKey that was missing from the load order
    /// </summary>
    public ModKey MissingModKey { get; }

    /// <summary>
    /// The FormKey that referenced the missing mod
    /// </summary>
    public FormKey ReferencedBy { get; }

    public ReferencedModMissingError(
        IList<ILoadOrderListingGetter> loadOrder,
        ModKey missingModKey,
        FormKey referencedBy)
    {
        LoadOrder = loadOrder;
        MissingModKey = missingModKey;
        ReferencedBy = referencedBy;
    }
}
