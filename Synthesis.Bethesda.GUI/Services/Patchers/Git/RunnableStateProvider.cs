using System;
using System.Reactive.Linq;
using Noggog;
using Noggog.Reactive;
using Synthesis.Bethesda.Execution.Patchers.Git;

namespace Synthesis.Bethesda.GUI.Services.Patchers.Git
{
    public interface IRunnableStateProvider
    {
        IObservable<ConfigurationState<RunnerRepoInfo>> State { get; }
    }

    public class RunnableStateProvider : IRunnableStateProvider
    {
        public IObservable<ConfigurationState<RunnerRepoInfo>> State { get; }

        public RunnableStateProvider(
            ISchedulerProvider schedulerProvider,
            ICheckoutInputProvider checkoutInputProvider,
            IPrepareRunnableState prepareRunnableState)
        {
            State = checkoutInputProvider.Input
                .Throttle(TimeSpan.FromMilliseconds(150), schedulerProvider.MainThread)
                .DistinctUntilChanged()
                .ObserveOn(schedulerProvider.TaskPool)
                .Select(prepareRunnableState.Prepare)
                .Switch()
                .StartWith(new ConfigurationState<RunnerRepoInfo>(GetResponse<RunnerRepoInfo>.Fail("Constructing runnable state"))
                {
                    IsHaltingError = false
                })
                .Replay(1)
                .RefCount();
        }
    }
}