using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Noggog;
using Noggog.Reactive;
using Serilog;
using Synthesis.Bethesda.Execution.GitRespository;
using Synthesis.Bethesda.Execution.Logging;
using Synthesis.Bethesda.Execution.Patchers.Solution;

namespace Synthesis.Bethesda.Execution.Patchers.Git
{
    public interface IPrepareDriverRepository
    {
        IObservable<ConfigurationState<DriverRepoInfo>> Get(
            IObservable<ConfigurationState<string>> remoteRepoPath);
    }

    public class PrepareDriverRepository : IPrepareDriverRepository
    {
        private readonly ILogger _logger;
        private readonly IProvideRepositoryCheckouts _repoCheckouts;
        private readonly IPathToSolutionProvider _pathToSolutionProvider;
        private readonly ICheckOrCloneRepo _checkOrClone;
        private readonly IDriverRepoDirectoryProvider _driverRepoDirectoryProvider;
        private readonly ISchedulerProvider _schedulerProvider;

        public PrepareDriverRepository(
            ILogger logger,
            IProvideRepositoryCheckouts repoCheckouts,
            IPathToSolutionProvider pathToSolutionProvider,
            ICheckOrCloneRepo checkOrClone,
            IDriverRepoDirectoryProvider driverRepoDirectoryProvider,
            ISchedulerProvider schedulerProvider)
        {
            _logger = logger;
            _repoCheckouts = repoCheckouts;
            _pathToSolutionProvider = pathToSolutionProvider;
            _checkOrClone = checkOrClone;
            _driverRepoDirectoryProvider = driverRepoDirectoryProvider;
            _schedulerProvider = schedulerProvider;
        }
        
        public IObservable<ConfigurationState<DriverRepoInfo>> Get(IObservable<ConfigurationState<string>> remoteRepoPath)
        {
            return remoteRepoPath
                .Throttle(TimeSpan.FromMilliseconds(100), _schedulerProvider.MainThread)
                .ObserveOn(_schedulerProvider.TaskPool)
                .SelectReplaceWithIntermediate(
                    new ConfigurationState<DriverRepoInfo>(default!)
                    {
                        IsHaltingError = false,
                        RunnableState = ErrorResponse.Fail("Cloning driver repository"),
                    },
                    async (path, cancel) =>
                    {
                        if (!path.IsHaltingError && path.RunnableState.Failed)
                            return path.BubbleError<DriverRepoInfo>();
                        using var timing = _logger.Time("Cloning driver repository");

                        var driverRepoPath = _driverRepoDirectoryProvider.Path;

                        // Clone and/or double check the clone is correct
                        var state = _checkOrClone.Check(path.ToGetResponse(), driverRepoPath,
                            (x) => _logger.Information(x), cancel);
                        if (state.Failed)
                        {
                            _logger.Error($"Failed to check out driver repository: {state.Reason}");
                            return new ConfigurationState<DriverRepoInfo>(default!, (ErrorResponse) state);
                        }

                        cancel.ThrowIfCancellationRequested();

                        // Grab all the interesting metadata
                        List<(int Index, string Name, string Sha)> tags;
                        Dictionary<string, string> branchShas;
                        string masterBranch;
                        try
                        {
                            using var repoCheckout = _repoCheckouts.Get(driverRepoPath);
                            var repo = repoCheckout.Repository;
                            var master = repo.MainBranch;
                            if (master == null)
                            {
                                _logger.Error($"Failed to check out driver repository: Could not locate master branch");
                                return new ConfigurationState<DriverRepoInfo>(default!,
                                    ErrorResponse.Fail("Could not locate master branch."));
                            }

                            masterBranch = master.FriendlyName;
                            repo.ResetHard();
                            repo.Checkout(master);
                            repo.Pull();
                            tags = repo.Tags.Select(tag => (tag.FriendlyName, tag.Target.Sha))
                                .WithIndex()
                                .Select(x => (x.Index, x.Item.FriendlyName, x.Item.Sha))
                                .ToList();
                            branchShas = repo.Branches
                                .ToDictionary(x => x.FriendlyName, x => x.Tip.Sha, StringComparer.OrdinalIgnoreCase);
                        }
                        catch (Exception ex)
                        {
                            _logger.Error(ex, $"Failed to check out driver repository");
                            return new ConfigurationState<DriverRepoInfo>(default!, ErrorResponse.Fail(ex));
                        }

                        // Try to locate a solution to drive from
                        var slnPath = _pathToSolutionProvider.Path;
                        if (slnPath == null)
                        {
                            _logger.Error($"Failed to check out driver repository: Could not locate solution to run.");
                            return new ConfigurationState<DriverRepoInfo>(default!,
                                ErrorResponse.Fail("Could not locate solution to run."));
                        }

                        var availableProjs = SolutionPatcherRun.AvailableProjectSubpaths(slnPath).ToList();
                        return new ConfigurationState<DriverRepoInfo>(
                            new DriverRepoInfo(
                                slnPath: slnPath,
                                masterBranchName: masterBranch,
                                branchShas: branchShas,
                                tags: tags,
                                availableProjects: availableProjs));
                    });
        }
    }
}