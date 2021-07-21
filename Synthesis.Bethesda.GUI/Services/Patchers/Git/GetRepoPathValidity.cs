using System;
using System.Reactive.Linq;
using LibGit2Sharp;
using Noggog;
using Noggog.Reactive;
using Synthesis.Bethesda.Execution.Patchers.Git;

namespace Synthesis.Bethesda.GUI.Services.Patchers.Git
{
    public interface IGetRepoPathValidity
    {
        IObservable<ConfigurationState<string>> Get();
    }

    public class GetRepoPathValidity : IGetRepoPathValidity
    {
        private readonly IRemoteRepoPathProvider _remoteRepoPathProvider;
        private readonly ICheckRepoIsValid _checkRepoIsValid;
        private readonly ISchedulerProvider _schedulerProvider;

        public GetRepoPathValidity(
            IRemoteRepoPathProvider remoteRepoPathProvider,
            ICheckRepoIsValid checkRepoIsValid,
            ISchedulerProvider schedulerProvider)
        {
            _remoteRepoPathProvider = remoteRepoPathProvider;
            _checkRepoIsValid = checkRepoIsValid;
            _schedulerProvider = schedulerProvider;
        }
        
        public IObservable<ConfigurationState<string>> Get()
        {
            var replay = _remoteRepoPathProvider.Path
                .Replay()
                .AutoConnect(2);
            
            // Check to see if remote path points to a reachable git repository
            return replay
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
                        if (_checkRepoIsValid.IsValidRepository(p))
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