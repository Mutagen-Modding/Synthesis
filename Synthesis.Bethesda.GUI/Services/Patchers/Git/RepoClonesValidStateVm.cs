using System.Reactive.Linq;
using Noggog.Reactive;
using Noggog.WPF;
using ReactiveUI;

namespace Synthesis.Bethesda.GUI.Services.Patchers.Git;

public interface IRepoClonesValidStateVm
{
    bool Valid { get; }
}

public class RepoClonesValidStateVm : ViewModel, IRepoClonesValidStateVm
{
    private readonly ObservableAsPropertyHelper<bool> _Valid;
    public bool Valid => _Valid.Value;

    public RepoClonesValidStateVm(
        IDriverRepositoryPreparationFollower driverRepositoryPreparation,
        IRunnerRepositoryPreparation runnerRepositoryPreparation,
        ISchedulerProvider schedulerProvider)
    {
        _Valid = Observable.CombineLatest(
                driverRepositoryPreparation.DriverInfo,
                runnerRepositoryPreparation.State,
                (driver, runner) => driver.RunnableState.Succeeded && runner.RunnableState.Succeeded)
            .ToGuiProperty(this, nameof(Valid), schedulerProvider.MainThread, deferSubscription: true);
    }
}