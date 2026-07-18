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
    public IReadOnlyList<ModKey> LoadOrder { get; }

    public NonAdjacentSplitModsErrorClassification(ModKey baseModKey, IReadOnlyList<ModKey> splitModKeys, IReadOnlyList<ModKey> loadOrder)
    {
        BaseModKey = baseModKey;
        SplitModKeys = splitModKeys;
        LoadOrder = loadOrder;
    }

    public override string ErrorType => ErrorTypeString;

    public override string Message => "Please ensure all split files are consecutive in your plugins.txt with no other mods between them.";

    public override void LogCliDetails(Action<string> log)
    {
        log($"Base mod: {BaseModKey}");
        log("Split files that must be adjacent:");
        foreach (var mod in SplitModKeys)
        {
            log($"  - {mod}");
        }
        if (LoadOrder.Count > 0)
        {
            log("Load order at time of error:");
            foreach (var mod in LoadOrder)
            {
                log($"  - {mod}");
            }
        }
    }
}
