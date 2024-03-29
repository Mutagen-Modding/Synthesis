using Mutagen.Bethesda.Plugins;

namespace Mutagen.Bethesda.Synthesis;

public class UserPreferences
{
    /// <summary>
    /// An enumerable of ModKeys to allow into the patcher.<br />
    /// Any mod not on the list will not be present in the supplied load order<br />
    /// Null means to include all mods
    /// </summary>
    public IEnumerable<ModKey>? InclusionMods { get; set; }

    /// <summary>
    /// An enumerable of ModKeys to disallow into the patcher.<br />
    /// Any mod on the list will not be present in the supplied load order<br />
    /// Null means to include all mods
    /// </summary>
    public IEnumerable<ModKey>? ExclusionMods { get; set; }

    /// <summary>
    /// Whether to include mods on the LoadOrder that are not enabled
    /// </summary>
    public bool IncludeDisabledMods { get; set; }

    /// <summary>
    /// Should disabled mods that are referenced by active mods be marked active themselves?
    /// </summary>
    public bool AddImplicitMasters { get; set; } = true;

    /// <summary>
    /// If program args are empty, what actions should be taken
    /// </summary>
    public RunDefaultPatcher? ActionsForEmptyArgs { get; set; } = null;

    /// <summary>
    /// Optional cancellation token that will signal to the patcher to stop early
    /// </summary>
    public CancellationToken Cancel { get; set; } = CancellationToken.None;

    /// <summary>
    /// Indicates that a patcher does not intend to make any changes to the patch directly <br/>
    /// Note: A SynthesisState's PatchMod object may not be set if this is turned on. <br/>
    /// Instead, the previous patch mod can be retrieved as the last mod in the load order.
    /// </summary>
    public bool NoPatch { get; set; }

    public PatcherPreferences ToPatcherPrefs()
    {
        return new PatcherPreferences()
        {
            AddImplicitMasters = AddImplicitMasters,
            ExclusionMods = ExclusionMods,
            IncludeDisabledMods = IncludeDisabledMods,
            InclusionMods = InclusionMods,
            NoPatch = NoPatch,
            Cancel = Cancel,
        };
    }

    public RunPreferences ToRunPrefs()
    {
        return new RunPreferences()
        {
            ActionsForEmptyArgs = ActionsForEmptyArgs
        };
    }
}