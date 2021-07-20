using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using Noggog;
using Serilog;
using Synthesis.Bethesda.Execution.DotNet;

namespace Synthesis.Bethesda.Execution.Patchers.Git
{
    public interface IPerformGitPatcherCompilation
    {
        IObservable<ConfigurationState<RunnerRepoInfo>> Process(
            IObservable<ConfigurationState<RunnerRepoInfo>> runnableState);
    }

    public class PerformGitPatcherCompilation : IPerformGitPatcherCompilation
    {
        private readonly IBuild _build;
        private readonly ILogger _logger;

        public PerformGitPatcherCompilation(
            IBuild build,
            ILogger logger)
        {
            _build = build;
            _logger = logger;
        }
        
        public IObservable<ConfigurationState<RunnerRepoInfo>> Process(
            IObservable<ConfigurationState<RunnerRepoInfo>> runnableState)
        {
            return runnableState
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
                            var compileResp = await _build.Compile(state.Item.ProjPath, cancel, _logger.Information);
                            if (compileResp.Failed)
                            {
                                _logger.Information($"Compiling failed: {compileResp.Reason}");
                                List<string> errs = new List<string>();
                                DotNetCommands.PrintErrorMessage(compileResp.Reason,
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
                            var str = $"Error checking out runner repository: {ex}";
                            _logger.Error(str);
                            observer.OnNext(ErrorResponse.Fail(str).BubbleFailure<RunnerRepoInfo>());
                        }

                        observer.OnCompleted();
                    });
                })
                .Switch()
                .StartWith(new ConfigurationState<RunnerRepoInfo>(
                    GetResponse<RunnerRepoInfo>.Fail("Compilation uninitialized")));
        }
    }
}