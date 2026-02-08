using Mutagen.Bethesda.Plugins;

namespace Synthesis.Bethesda.Execution.Reporters.Classifications;

/// <summary>
/// Classification for when split mod files are not adjacent in the load order.
/// </summary>
public class NonAdjacentSplitModsErrorClassification : ErrorClassification
{
    public const string ErrorTypeString = "Non-Adjacent Split Mods";

    public ModKey BaseModKey { get; }
    public IReadOnlyList<ModKey> SplitModKeys { get; }

    public NonAdjacentSplitModsErrorClassification(ModKey baseModKey, IReadOnlyList<ModKey> splitModKeys)
    {
        BaseModKey = baseModKey;
        SplitModKeys = splitModKeys;
    }

    public override string ErrorType => ErrorTypeString;

    public override string Message => $"Split mod files for '{BaseModKey}' must be adjacent in the load order.\n\n" +
                                      $"Found split files: {string.Join(", ", SplitModKeys)}\n\n" +
                                      "Please ensure all split files are consecutive in your plugins.txt with no other mods between them.";
}
