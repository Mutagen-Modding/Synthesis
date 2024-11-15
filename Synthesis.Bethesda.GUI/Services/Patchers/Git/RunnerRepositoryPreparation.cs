﻿using System.Reactive.Linq;
using Noggog;
using Noggog.Reactive;
using Serilog;
using Noggog.GitRepository;
using Synthesis.Bethesda.Execution.GitRepository;
using Synthesis.Bethesda.Execution.Logging;
using Synthesis.Bethesda.Execution.Patchers.Git.Services;

namespace Synthesis.Bethesda.GUI.Services.Patchers.Git;

public interface IRunnerRepositoryPreparation
{
    IObservable<ConfigurationState> State { get; }
}

public class RunnerRepositoryPreparation : IRunnerRepositoryPreparation
{
    public IObservable<ConfigurationState> State { get; }
        
    public RunnerRepositoryPreparation(
        ILogger logger,
        ICheckOrCloneRepo checkOrClone,
        ICheckIfRepositoryDesirable checkIfRepositoryDesirable,
        IRunnerRepoDirectoryProvider runnerRepoDirectoryProvider,
        IGetRepoPathValidity getRepoPathValidity,
        ISchedulerProvider schedulerProvider)
    {
        State = getRepoPathValidity.RepoPath
            .Throttle(TimeSpan.FromMilliseconds(100), schedulerProvider.MainThread)
            .ObserveOn(schedulerProvider.TaskPool)
            .SelectReplaceWithIntermediate(
                new ConfigurationState(ErrorResponse.Fail("Cloning runner repository"))
                {
                    IsHaltingError = false
                },
                async (path, cancel) =>
                {
                    if (path.RunnableState.Failed) return path.ToUnit();
                    using var timing = logger.Time($"runner repo: {path.Item}");
                    return (ErrorResponse)checkOrClone.Check(
                        path.ToGetResponse(),
                        runnerRepoDirectoryProvider.Path, 
                        checkIfRepositoryDesirable.IsDesirable,
                        cancel);
                })
            .Replay(1)
            .RefCount();
    }
}