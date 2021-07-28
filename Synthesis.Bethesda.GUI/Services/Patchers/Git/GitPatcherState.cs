using System;
using System.Reactive.Linq;
using DynamicData;
using Mutagen.Bethesda.Plugins;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using Serilog;
using Synthesis.Bethesda.Execution.DotNet;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.GUI.ViewModels.Top;

namespace Synthesis.Bethesda.GUI.Services.Patchers.Git
{
    public interface IGitPatcherState
    {
        IObservable<ConfigurationState> State { get; }
    }

    public class GitPatcherState : IGitPatcherState
    {
        public IObservable<ConfigurationState> State { get; }

        public GitPatcherState(
            IDriverRepositoryPreparationFollower driverRepositoryPreparation,
            IRunnerRepositoryPreparation runnerRepositoryState,
            IRunnableStateProvider runnableStateProvider,
            IPatcherRunnabilityCliState runnabilityCliState,
            IInstalledSdkFollower dotNetInstalled,
            IEnvironmentErrorsVm envErrors,
            IMissingMods missingMods,
            ILogger logger)
        {
            State = Observable.CombineLatest(
                    driverRepositoryPreparation.DriverInfo
                        .Select(x => x.ToUnit()),
                    runnerRepositoryState.State,
                    runnableStateProvider.State
                        .Select(x => x.ToUnit()),
                    runnabilityCliState.Runnable,
                    dotNetInstalled.DotNetSdkInstalled
                        .Select(x => (x, true))
                        .StartWith((new DotNetVersion(string.Empty, false), false)),
                    envErrors.WhenAnyFallback(x => x.ActiveError!.ErrorString),
                    missingMods.Missing
                        .QueryWhenChanged()
                        .Throttle(TimeSpan.FromMilliseconds(200), RxApp.MainThreadScheduler)
                        .StartWith(ListExt.Empty<ModKey>()),
                    (driver, runner, checkout, runnability, dotnet, envError, reqModsMissing) =>
                    {
                        if (driver.IsHaltingError) return driver;
                        if (runner.IsHaltingError) return runner;
                        if (!dotnet.Item2)
                        {
                            return new ConfigurationState(ErrorResponse.Fail("Determining DotNet SDK installed"))
                            {
                                IsHaltingError = false
                            };
                        }

                        if (!dotnet.Item1.Acceptable)
                            return new ConfigurationState(ErrorResponse.Fail("No DotNet SDK installed"));
                        if (envError != null)
                        {
                            return new ConfigurationState(ErrorResponse.Fail(envError));
                        }

                        if (reqModsMissing.Count > 0)
                        {
                            return new ConfigurationState(ErrorResponse.Fail(
                                $"Required mods missing from load order:{Environment.NewLine}{string.Join(Environment.NewLine, reqModsMissing)}"));
                        }

                        if (runnability.RunnableState.Failed)
                        {
                            return runnability.BubbleError();
                        }

                        if (checkout.RunnableState.Failed)
                        {
                            return checkout.BubbleError();
                        }

                        logger.Information("State returned success!");
                        return ConfigurationState.Success;
                    })
                .Replay(1)
                .RefCount();
        }
    }
}