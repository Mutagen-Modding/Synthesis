using System;
using System.Reactive.Linq;
using LibGit2Sharp;
using Noggog;
using Noggog.Reactive;

namespace Synthesis.Bethesda.Execution.Patchers.Git
{
    public interface IGetRepoPathValidity
    {
        IObservable<ConfigurationState<string>> Get(IObservable<string> repoPath);
    }

    public class GetRepoPathValidity : IGetRepoPathValidity
    {
        private readonly ISchedulerProvider _schedulerProvider;

        public GetRepoPathValidity(ISchedulerProvider schedulerProvider)
        {
            _schedulerProvider = schedulerProvider;
        }
        
        public IObservable<ConfigurationState<string>> Get(IObservable<string> repoPath)
        {
            // Check to see if remote path points to a reachable git repository
            return repoPath
                .DistinctUntilChanged()
                .Select(x => new ConfigurationState<string>(string.Empty)
                {
                    IsHaltingError = false,
                    RunnableState = ErrorResponse.Fail("Checking remote repository correctness.")
                })
                // But merge in the work of checking the repo on that same path to get the eventual result
                .Merge(repoPath
                    .DistinctUntilChanged()
                    .Debounce(TimeSpan.FromMilliseconds(300), _schedulerProvider.MainThread)
                    .ObserveOn(_schedulerProvider.TaskPool)
                    .Select(p =>
                    {
                        try
                        {
                            if (Repository.ListRemoteReferences(p).Any()) return new ConfigurationState<string>(p);
                        }
                        catch (Exception)
                        {
                        }
                        return new ConfigurationState<string>(string.Empty, ErrorResponse.Fail("Path does not point to a valid repository."));
                    }));
        }
    }
}