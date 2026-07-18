using Synthesis.Bethesda.Execution.Reporters.Classifications;
using Synthesis.Bethesda.GUI.Services.Main;

namespace Synthesis.Bethesda.GUI.ViewModels.Errors;

/// <summary>
/// View model wrapper for NonAdjacentSplitModsErrorClassification
/// </summary>
public class NonAdjacentSplitModsErrorVm : ErrorClassificationVm
{
    private readonly NonAdjacentSplitModsErrorClassification _error;

    /// <summary>
    /// The base mod key that has split files
    /// </summary>
    public string BaseModKey => _error.BaseModKey.ToString();

    /// <summary>
    /// The list of split mod keys for display
    /// </summary>
    public IList<SplitModItem> SplitMods { get; }

    /// <summary>
    /// The full load order for reference
    /// </summary>
    public IList<LoadOrderItem> LoadOrder { get; }

    public delegate NonAdjacentSplitModsErrorVm Factory(NonAdjacentSplitModsErrorClassification error);

    public NonAdjacentSplitModsErrorVm(
        NonAdjacentSplitModsErrorClassification error,
        INavigateTo navigateTo)
        : base(error, navigateTo)
    {
        _error = error;

        // Convert split mods to display items
        SplitMods = error.SplitModKeys
            .Select(x => new SplitModItem(x.ToString()))
            .ToList();

        // Convert load order to display items
        LoadOrder = error.LoadOrder
            .Select(x => new LoadOrderItem(x.ToString()))
            .ToList();
    }

    public record SplitModItem(string ModKey);
    public record LoadOrderItem(string ModKey);
}
