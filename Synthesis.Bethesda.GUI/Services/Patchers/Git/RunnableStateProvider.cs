using System.Reactive.Linq;
using Noggog;
using Noggog.Reactive;
using Noggog.WPF;
using ReactiveUI;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Patchers.Git.Services;
using Synthesis.Bethesda.Execution.Patchers.Solution;

namespace Synthesis.Bethesda.GUI.Services.Patchers.Git;

public interface IRunnableStateProvider
{
    ConfigurationState<RunnerRepoInfo> State { get; }
}

public class RunnableStateProvider : ViewModel, IRunnableStateProvider, IPathToProjProvider
{
    private readonly ObservableAsPropertyHelper<ConfigurationState<RunnerRepoInfo>> _state;
    public ConfigurationState<RunnerRepoInfo> State => _state.Value;

    public RunnableStateProvider(
        ISchedulerProvider schedulerProvider,
        ICheckoutInputProvider checkoutInputProvider,
        IPrepareRunnableState prepareRunnableState)
    {
        _state = checkoutInputProvider.Input
            .Throttle(TimeSpan.FromMilliseconds(150), schedulerProvider.MainThread)
            .DistinctUntilChanged()
            .ObserveOn(schedulerProvider.TaskPool)
            .Select(prepareRunnableState.Prepare)
            .Switch()
            .ToGuiProperty(this, nameof(State), new ConfigurationState<RunnerRepoInfo>(
                GetResponse<RunnerRepoInfo>.Fail("Constructing runnable state"))
            {
                IsHaltingError = false
            }, deferSubscription: true);
    }
            
    FilePath IPathToProjProvider.Path => State?.Item?.Project.ProjPath ??
                                         throw new ArgumentNullException($"{nameof(IPathToProjProvider)}.{nameof(IPathToProjProvider.Path)}");
}