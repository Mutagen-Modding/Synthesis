using System.Reactive.Linq;
using Noggog.WPF;
using Serilog;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.Confirmations;
using Synthesis.Bethesda.GUI.ViewModels.Top;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles;

public class PatcherInitRenameValidator
{
    private readonly ILogger _logger;
    private readonly IProfileGroupsList _groupsList;
    private readonly IConfirmationPanelControllerVm _confirmation;

    public PatcherInitRenameValidator(
        ILogger logger,
        IProfileGroupsList groupsList,
        IConfirmationPanelControllerVm confirmation)
    {
        _logger = logger;
        _groupsList = groupsList;
        _confirmation = confirmation;
    }

    public async Task<bool> ConfirmNameUnique(PatcherVm patcher)
    {
        var existingNames = _groupsList.Groups.Items
            .SelectMany(g => g.Patchers.Items)
            .Select(x => x.NameVm.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!existingNames.Contains(patcher.NameVm.Name)) return true;
        
        _logger.Warning("Duplicate name detected for new patcher: {Name}.  Initiating rename request.", patcher.NameVm.Name);

        var rename = new PatcherInitRenameActionVm(
            patcher.NameVm.Name,
            existingNames);
        _confirmation.TargetConfirmation = rename;
        var signal = rename.ConfirmActionCommand.EndingExecution().Select(_ => true);
        if (rename.DiscardActionCommand != null)
        {
            signal = signal.Merge(rename.DiscardActionCommand.EndingExecution().Select(_ => false));
        }

        var result = await signal.Take(1);
        if (!result)
        {
            _logger.Warning("Rename was not successful for {Name}.  Backing out", patcher.NameVm.Name);
            return false;
        }
        
        _logger.Information("Rename was successful for {Name} to {Replacement}", patcher.NameVm.Name, rename.Name);
        patcher.NameVm.Nickname = rename.Name;
        return true;
    }
}