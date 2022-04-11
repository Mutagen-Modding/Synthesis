using System;
using System.Reactive.Linq;
using Noggog;
using Synthesis.Bethesda.GUI.Services.Patchers.Solution;

namespace Synthesis.Bethesda.GUI.Services.Patchers.Git;

public interface ICheckoutInputProvider
{
    IObservable<PotentialCheckoutInput> Input { get; }
}

public class CheckoutInputProvider : ICheckoutInputProvider
{
    public IObservable<PotentialCheckoutInput> Input { get; }

    public CheckoutInputProvider(
        IRunnerRepositoryPreparation runnerRepositoryState,
        ISelectedProjectInputVm selectedProjectInput,
        IPatcherVersioningFollower patcherTargeting,
        INugetVersioningFollower nugetTargeting)
    {
        Input = Observable.CombineLatest(
                runnerRepositoryState.State,
                selectedProjectInput.Picker.PathState()
                    .Select(x => x.Succeeded ? x : GetResponse<string>.Fail("No patcher project selected.")),
                patcherTargeting.ActivePatcherVersion,
                nugetTargeting.ActiveNugetVersion,
                (runnerState, proj, patcherVersioning, libraryNugets) => new PotentialCheckoutInput(
                    runnerState,
                    proj,
                    patcherVersioning,
                    libraryNugets))
            .Replay(1)
            .RefCount();
    }
}