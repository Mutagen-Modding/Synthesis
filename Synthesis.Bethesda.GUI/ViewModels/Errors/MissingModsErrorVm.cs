using Synthesis.Bethesda.Execution.Reporters.Classifications;
using Synthesis.Bethesda.GUI.Services.Main;

namespace Synthesis.Bethesda.GUI.ViewModels.Errors;

/// <summary>
/// View model wrapper for MissingModsErrorClassification
/// </summary>
public class MissingModsErrorVm : ErrorClassificationVm
{
    private readonly MissingModsErrorClassification _error;

    /// <summary>
    /// The list of missing mod keys for display
    /// </summary>
    public IList<MissingModItem> MissingMods { get; }

    public delegate MissingModsErrorVm Factory(MissingModsErrorClassification error);

    public MissingModsErrorVm(
        MissingModsErrorClassification error,
        INavigateTo navigateTo)
        : base(error, navigateTo)
    {
        _error = error;

        // Convert missing mods to display items
        MissingMods = error.MissingMods
            .Select(x => new MissingModItem(x.ToString()))
            .ToList();
    }

    public record MissingModItem(string ModKey);
}
