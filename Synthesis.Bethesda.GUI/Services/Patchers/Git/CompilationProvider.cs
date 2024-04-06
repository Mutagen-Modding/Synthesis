using System.IO;
using System.Reactive.Linq;
using Noggog;
using ReactiveUI;
using Serilog;
using Synthesis.Bethesda.Execution.DotNet;
using Synthesis.Bethesda.Execution.Patchers.Git;
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
        IRunnableStateProvider runnableStateProvider)
    {
        State = runnableStateProvider.WhenAnyValue(x => x.State)
            .CombineLatest(
                shortCircuitSettingsProvider.WhenAnyValue(x => x.Shortcircuit),
                (state, _) => state)
            .Select(state =>
            {
                return Observable.Create<ConfigurationState<RunnerRepoInfo>>(async (observer, cancel) =>
                {
                    if (state.RunnableState.Failed)
                    {
                        observer.OnNext(state);
                        return;
                    }

                    try
                    {
                        logger.Information("Compiling {Target}", state.Item);
                        // Return early with the values, but mark not complete
                        observer.OnNext(new ConfigurationState<RunnerRepoInfo>(state.Item)
                        {
                            IsHaltingError = false,
                            RunnableState = ErrorResponse.Fail("Compiling")
                        });

                        // Compile to help prep
                        var compileResp = await build.Compile(state.Item, cancel).ConfigureAwait(false);
                        if (compileResp.Failed)
                        {
                            logger.Information("Compiling {Target} failed: {Reason}", state.Item, compileResp.Reason);
                            var errs = new List<string>();
                            printErrorMessage.Print(compileResp.Reason,
                                $"{Path.GetDirectoryName(state.Item.Project.ProjPath)}\\", (s, _) =>
                                {
                                    errs.Add(s.ToString());
                                });
                            observer.OnNext(
                                GetResponse<RunnerRepoInfo>.Fail(string.Join(Environment.NewLine, errs)));
                            return;
                        }

                        // Return things again, without error
                        logger.Information("Finished compiling {Target}", state.Item);
                        observer.OnNext(state);
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