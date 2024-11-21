using System.IO;
using System.Reactive.Linq;
using Noggog;
using ReactiveUI;
using Serilog;
using Synthesis.Bethesda.Execution.DotNet;
using Synthesis.Bethesda.Execution.DotNet.Singleton;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Patchers.Git.Services;
using Synthesis.Bethesda.Execution.Patchers.Running.Git;

namespace Synthesis.Bethesda.GUI.Services.Patchers.Git;

public interface ICompilationProvider
{
    IObservable<ConfigurationState<RunnerRepoInfo>> State { get; }
}

public class CompilationProvider : ICompilationProvider
{
    public IObservable<ConfigurationState<RunnerRepoInfo>> State { get; }

    public CompilationProvider(
        IGitPatcherCompilation build,
        ILogger logger,
        IPrintErrorMessage printErrorMessage,
        IShortCircuitSettingsProvider shortCircuitSettingsProvider,
        IInstalledSdkFollower installedSdkFollower,
        IRunnableStateProvider runnableStateProvider)
    {
        State = runnableStateProvider.WhenAnyValue(x => x.State)
            .CombineLatest(
                installedSdkFollower.DotNetSdkInstalled,
                shortCircuitSettingsProvider.WhenAnyValue(x => x.Shortcircuit),
                (State, DotNet, _) => (State, DotNet))
            .Select(x =>
            {
                return Observable.Create<ConfigurationState<RunnerRepoInfo>>(async (observer, cancel) =>
                {
                    if (x.State.RunnableState.Failed)
                    {
                        observer.OnNext(x.State);
                        return;
                    }

                    try
                    {
                        logger.Information("Compiling {Target}", x.State.Item);
                        // Return early with the values, but mark not complete
                        observer.OnNext(new ConfigurationState<RunnerRepoInfo>(x.State.Item)
                        {
                            IsHaltingError = false,
                            RunnableState = ErrorResponse.Fail("Compiling")
                        });

                        // Compile to help prep
                        var compileResp = await build.Compile(x.State.Item, x.DotNet, cancel).ConfigureAwait(false);
                        if (compileResp.Failed)
                        {
                            logger.Information("Compiling {Target} failed: {Reason}", x.State.Item, compileResp.Reason);
                            var errs = new List<string>();
                            printErrorMessage.Print(compileResp.Reason,
                                $"{Path.GetDirectoryName(x.State.Item.Project.ProjPath)}\\", (s, _) =>
                                {
                                    errs.Add(s.ToString());
                                });
                            observer.OnNext(
                                GetResponse<RunnerRepoInfo>.Fail(string.Join(Environment.NewLine, errs)));
                            return;
                        }

                        // Return things again, without error
                        logger.Information("Finished compiling {Target}", x.State.Item);
                        observer.OnNext(x.State);
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "Error compiling");
                        observer.OnNext(ErrorResponse.Fail($"Error compiling: {ex}").BubbleFailure<RunnerRepoInfo>());
                    }

                    observer.OnCompleted();
                });
            })
            .Switch()
            .StartWith(new ConfigurationState<RunnerRepoInfo>(
                GetResponse<RunnerRepoInfo>.Fail("Compilation uninitialized")))
            .Replay(1)
            .RefCount();
    }
}