using System.Reactive;
using Noggog.WPF;
using ReactiveUI;

namespace Synthesis.Bethesda.GUI.Services.Patchers.Git
{
    public interface IUpdateAllCommand
    {
        ReactiveCommand<Unit, Unit> Command { get; }
    }

    public class UpdateAllCommand : IUpdateAllCommand
    {
        public ReactiveCommand<Unit, Unit> Command { get; }

        public UpdateAllCommand(
            IGitPatcherTargetingVm patcherTargetingVm,
            IGitNugetTargetingVm nugetTargetingVm)
        {
            Command = CommandExt.CreateCombinedAny(
                nugetTargetingVm.UpdateMutagenManualToLatestCommand,
                nugetTargetingVm.UpdateSynthesisManualToLatestCommand,
                patcherTargetingVm.UpdateToBranchCommand,
                patcherTargetingVm.UpdateToTagCommand);
        }
    }
}