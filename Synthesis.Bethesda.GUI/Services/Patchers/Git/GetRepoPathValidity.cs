using System;
using System.Reactive.Linq;
using Noggog;
using Noggog.Reactive;
using Synthesis.Bethesda.Execution.GitRepository;
using Synthesis.Bethesda.Execution.Patchers.Git;

namespace Synthesis.Bethesda.GUI.Services.Patchers.Git
{
    public interface IGetRepoPathValidity
    {
        IObservable<ConfigurationState<string>> RepoPath { get; }
    }

    public class GetRepoPathValidity : IGetRepoPathValidity
    {
        private readonly IRemoteRepoPathFollower _remoteRepoPathFollower;
        private readonly ICheckOriginRepoIsValid _checkOriginRepoIsValid;
        private readonly ISchedulerProvider _schedulerProvider;
        public IObservable<ConfigurationState<string>> RepoPath { get; }

        public GetRepoPathValidity(
            IRemoteRepoPathFollower remoteRepoPathFollower,
            ICheckOriginRepoIsValid checkOriginRepoIsValid,
            ISchedulerProvider schedulerProvider)
        {
            _remoteRepoPathFollower = remoteRepoPathFollower;
            _checkOriginRepoIsValid = checkOriginRepoIsValid;
            _schedulerProvider = schedulerProvider;
            
            
            var replay = _remoteRepoPathFollower.Path
                .Replay()
                .AutoConnect(2);
            
            // Check to see if remote path points to a reachable git repository
            RepoPath = replay
                .DistinctUntilChanged()
                .Select(x => new ConfigurationState<string>(string.Empty)
                {
                    IsHaltingError = false,
                    RunnableState = ErrorResponse.Fail("Checking remote repository correctness.")
                })
                // But merge in the work of checking the repo on that same path to get the eventual result
                .Merge(replay
                    .DistinctUntilChanged()
                    .Debounce(TimeSpan.FromMilliseconds(300), _schedulerProvider.MainThread)
                    .ObserveOn(_schedulerProvider.TaskPool)
                    .Select(p =>
                    {
                        if (_checkOriginRepoIsValid.IsValidRepository(p))
                        {
                            return new ConfigurationState<string>(p);
                        }
                        return new ConfigurationState<string>(string.Empty, ErrorResponse.Fail("Path does not point to a valid repository."));
                    }))
                .Replay(1)
                .RefCount();
        }
    }
}