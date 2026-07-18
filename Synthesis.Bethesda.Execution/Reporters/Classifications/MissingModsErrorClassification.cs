using Mutagen.Bethesda.Plugins;

namespace Synthesis.Bethesda.Execution.Reporters.Classifications;

/// <summary>
/// Classification for when mods in the load order are missing from disk.
/// </summary>
public class MissingModsErrorClassification : ErrorClassification
{
    public const string ErrorTypeString = "Missing Mods";

    public IReadOnlyList<ModKey> MissingMods { get; }

    public MissingModsErrorClassification(IReadOnlyList<ModKey> missingMods)
    {
        MissingMods = missingMods;
    }

    public override string ErrorType => ErrorTypeString;

    public override string Message => "The following mods are listed in your load order but could not be found in your data folder. " +
                                      "Please ensure these mods are installed correctly, or remove them from your load order.";

    public override void LogCliDetails(Action<string> log)
    {
        log("Missing mods:");
        foreach (var mod in MissingMods)
        {
            log($"  - {mod}");
        }
    }
}
