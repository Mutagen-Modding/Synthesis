using Synthesis.Bethesda.Execution.Reporters;
using Synthesis.Bethesda.Execution.Reporters.Classifications;
using Synthesis.Bethesda.GUI.Services.Main;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles.Running;

/// <summary>
/// View model wrapper for ReferencedModMissingError
/// </summary>
public class ReferencedModMissingErrorVm : ErrorClassificationVm
{
    private readonly ReferencedModMissingError _error;

    // Expose specific error properties for binding
    public string MissingModKey => _error.MissingModKey.ToString();
    public string ReferencedBy => _error.ReferencedBy.ToString();
    public IList<LoadOrderItem> LoadOrder { get; }

    public delegate ReferencedModMissingErrorVm Factory(ReferencedModMissingError error);

    public ReferencedModMissingErrorVm(
        ReferencedModMissingError error,
        INavigateTo navigateTo)
        : base(error, navigateTo)
    {
        _error = error;

        // Convert load order to display items
        LoadOrder = error.LoadOrder
            .Select(x => new LoadOrderItem(x.ModKey.ToString()))
            .ToList();
    }

    public record LoadOrderItem(string ModKey);
}
