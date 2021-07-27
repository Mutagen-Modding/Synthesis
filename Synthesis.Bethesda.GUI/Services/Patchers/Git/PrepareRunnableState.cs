using System;
using System.Reactive.Linq;
using Noggog;
using Serilog;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Patchers.Git.PrepareRunner;
using Synthesis.Bethesda.Execution.Patchers.Solution;

namespace Synthesis.Bethesda.GUI.Services.Patchers.Git
{
    public interface IPrepareRunnableState
    {
        IObservable<ConfigurationState<RunnerRepoInfo>> Prepare(PotentialCheckoutInput checkoutInput);
    }

    public class PrepareRunnableState : IPrepareRunnableState
    {
        private readonly IPrepareRunnerRepository _prepareRunner;
        private readonly CopyOverExtraData.Factory _copyOverFactory;
        public ILogger Logger { get; }

        public PrepareRunnableState(
            ILogger logger,
            IPrepareRunnerRepository prepareRunner,
            CopyOverExtraData.Factory copyOverFactory)
        {
            _prepareRunner = prepareRunner;
            _copyOverFactory = copyOverFactory;
            Logger = logger;
        }
        
        public IObservable<ConfigurationState<RunnerRepoInfo>> Prepare(PotentialCheckoutInput checkoutInput)
        {
            return Observable.Create<ConfigurationState<RunnerRepoInfo>>(async (observer, cancel) =>
            {
                try
                {
                    if (checkoutInput.RunnerState.RunnableState.Failed)
                    {
                        observer.OnNext(checkoutInput.RunnerState.BubbleError<RunnerRepoInfo>());
                        return;
                    }

                    if (checkoutInput.Proj.Failed)
                    {
                        observer.OnNext(checkoutInput.Proj.BubbleFailure<RunnerRepoInfo>());
                        return;
                    }

                    if (checkoutInput.LibraryNugets.Failed)
                    {
                        observer.OnNext(checkoutInput.LibraryNugets.BubbleFailure<RunnerRepoInfo>());
                        return;
                    }

                    observer.OnNext(new ConfigurationState<RunnerRepoInfo>(default!)
                    {
                        RunnableState = ErrorResponse.Fail("Checking out the proper commit"),
                        IsHaltingError = false,
                    });

                    var runInfo = await _prepareRunner.Checkout(
                        new CheckoutInput(
                            checkoutInput.Proj.Value,
                            checkoutInput.PatcherVersioning,
                            checkoutInput.LibraryNugets.Value),
                        cancel: cancel);

                    if (runInfo.RunnableState.Failed)
                    {
                        Logger.Error("Checking out runner repository failed: {Reason}", runInfo.RunnableState.Reason);
                        observer.OnNext(runInfo);
                        return;
                    }

                    Logger.Error("Checking out runner repository succeeded");

                    _copyOverFactory(
                        new DefaultDataPathProvider(
                            new PathToProjInjection {Path = runInfo.Item.ProjPath}))
                        .Copy();

                    observer.OnNext(runInfo);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error checking out runner repository");
                    observer.OnNext(ErrorResponse.Fail($"Error checking out runner repository: {ex}").BubbleFailure<RunnerRepoInfo>());
                }

                observer.OnCompleted();
            });
        }
    }
}