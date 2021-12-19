using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using Noggog;
using ReactiveUI;
using Serilog;
using Synthesis.Bethesda.Execution.DotNet;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Patchers.Running.Git;

namespace Synthesis.Bethesda.GUI.Services.Patchers.Git
{
    public interface ICompilationProvider
    {
        IObservable<ConfigurationState<RunnerRepoInfo>> State { get; }
    }

    public class CompilationProvider : ICompilationProvider
    {
        private readonly IGitPatcherCompilation _build;
        private readonly ILogger _logger;
        private readonly IRunnableStateProvider _runnableStateProvider;
        
        public IObservable<ConfigurationState<RunnerRepoInfo>> State { get; }

        public CompilationProvider(
            IGitPatcherCompilation build,
            ILogger logger,
            IPrintErrorMessage printErrorMessage,
            IShortCircuitSettingsProvider shortCircuitSettingsProvider,
            IRunnableStateProvider runnableStateProvider)
        {
            _build = build;
            _logger = logger;
            _runnableStateProvider = runnableStateProvider;
            
            State = _runnableStateProvider.WhenAnyValue(x => x.State)
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
                            _logger.Information("Compiling");
                            // Return early with the values, but mark not complete
                            observer.OnNext(new ConfigurationState<RunnerRepoInfo>(state.Item)
                            {
                                IsHaltingError = false,
                                RunnableState = ErrorResponse.Fail("Compiling")
                            });

                            // Compile to help prep
                            var compileResp = await _build.Compile(state.Item, cancel).ConfigureAwait(false);
                            if (compileResp.Failed)
                            {
                                _logger.Information("Compiling failed: {Reason}", compileResp.Reason);
                                var errs = new List<string>();
                                printErrorMessage.Print(compileResp.Reason,
                                    $"{Path.GetDirectoryName(state.Item.ProjPath)}\\", (s, _) =>
                                    {
                                        errs.Add(s.ToString());
                                    });
                                observer.OnNext(
                                    GetResponse<RunnerRepoInfo>.Fail(string.Join(Environment.NewLine, errs)));
                                return;
                            }

                            // Return things again, without error
                            _logger.Information("Finished compiling");
                            observer.OnNext(state);
                        }
                        catch (Exception ex)
                        {
                            _logger.Error(ex, "Error compiling");
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
}