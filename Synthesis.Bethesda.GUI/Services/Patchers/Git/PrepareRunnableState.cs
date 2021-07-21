using System;
using System.Reactive.Linq;
using Noggog;
using Serilog;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Patchers.Solution;

namespace Synthesis.Bethesda.GUI.Services.Patchers.Git
{
    public interface IPrepareRunnableState
    {
        IObservable<ConfigurationState<RunnerRepoInfo>> Prepare(
            ConfigurationState runnerState,
            GetResponse<string> proj,
            GitPatcherVersioning patcherVersioning,
            GetResponse<NugetVersioningTarget> libraryNugets);
    }

    public class PrepareRunnableState : IPrepareRunnableState
    {
        private readonly ICheckoutRunnerRepository _checkoutRunner;
        private readonly IRunnerRepoDirectoryProvider _runnerRepoDirectoryProvider;
        private readonly CopyOverExtraData.Factory _copyOverFactory;
        public ILogger Logger { get; }

        public PrepareRunnableState(
            ILogger logger,
            ICheckoutRunnerRepository checkoutRunner,
            IRunnerRepoDirectoryProvider runnerRepoDirectoryProvider,
            CopyOverExtraData.Factory copyOverFactory)
        {
            _checkoutRunner = checkoutRunner;
            _runnerRepoDirectoryProvider = runnerRepoDirectoryProvider;
            _copyOverFactory = copyOverFactory;
            Logger = logger;
        }
        
        public IObservable<ConfigurationState<RunnerRepoInfo>> Prepare(
            ConfigurationState runnerState,
            GetResponse<string> proj,
            GitPatcherVersioning patcherVersioning,
            GetResponse<NugetVersioningTarget> libraryNugets)
        {
            return Observable.Create<ConfigurationState<RunnerRepoInfo>>(async (observer, cancel) =>
            {
                try
                {
                    if (runnerState.RunnableState.Failed)
                    {
                        observer.OnNext(runnerState.BubbleError<RunnerRepoInfo>());
                        return;
                    }

                    if (proj.Failed)
                    {
                        observer.OnNext(proj.BubbleFailure<RunnerRepoInfo>());
                        return;
                    }

                    if (libraryNugets.Failed)
                    {
                        observer.OnNext(libraryNugets.BubbleFailure<RunnerRepoInfo>());
                        return;
                    }

                    observer.OnNext(new ConfigurationState<RunnerRepoInfo>(default!)
                    {
                        RunnableState = ErrorResponse.Fail("Checking out the proper commit"),
                        IsHaltingError = false,
                    });

                    var runInfo = await _checkoutRunner.Checkout(
                        proj: proj.Value,
                        localRepoDir: _runnerRepoDirectoryProvider.Path,
                        patcherVersioning: patcherVersioning,
                        nugetVersioning: libraryNugets.Value,
                        logger: (s) => Logger.Information(s),
                        cancel: cancel,
                        compile: false);

                    if (runInfo.RunnableState.Failed)
                    {
                        Logger.Error($"Checking out runner repository failed: {runInfo.RunnableState.Reason}");
                        observer.OnNext(runInfo);
                        return;
                    }

                    Logger.Error($"Checking out runner repository succeeded");

                    _copyOverFactory(
                        new DefaultDataPathProvider(
                            new PathToProjInjection {Path = runInfo.Item.ProjPath}))
                        .Copy(Logger.Information);

                    observer.OnNext(runInfo);
                }
                catch (Exception ex)
                {
                    var str = $"Error checking out runner repository: {ex}";
                    Logger.Error(str);
                    observer.OnNext(ErrorResponse.Fail(str).BubbleFailure<RunnerRepoInfo>());
                }

                observer.OnCompleted();
            });
        }
    }
}