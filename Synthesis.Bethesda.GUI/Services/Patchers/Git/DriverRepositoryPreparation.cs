using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Noggog;
using Noggog.Reactive;
using Serilog;
using Synthesis.Bethesda.Execution.GitRepository;
using Synthesis.Bethesda.Execution.Logging;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Patchers.Running;
using Synthesis.Bethesda.Execution.Patchers.Solution;
using Synthesis.Bethesda.GUI.Services.Patchers.Solution;

namespace Synthesis.Bethesda.GUI.Services.Patchers.Git
{
    public interface IDriverRepositoryPreparation : ISolutionFilePathFollower
    {
        IObservable<ConfigurationState<DriverRepoInfo>> DriverInfo { get; }
    }

    public class DriverRepositoryPreparation : IDriverRepositoryPreparation
    {
        private readonly ILogger _logger;
        private readonly IGetRepoPathValidity _getRepoPathValidity;
        private readonly IProvideRepositoryCheckouts _repoCheckouts;
        private readonly ISolutionFileLocator _solutionFileLocator;
        private readonly ICheckOrCloneRepo _checkOrClone;
        private readonly IDriverRepoDirectoryProvider _driverRepoDirectoryProvider;
        private readonly ISchedulerProvider _schedulerProvider;
        
        public IObservable<ConfigurationState<DriverRepoInfo>> DriverInfo { get; }

        IObservable<FilePath> ISolutionFilePathFollower.Path => DriverInfo
            .Select(x => x.Item?.SolutionPath ?? default);

        public DriverRepositoryPreparation(
            ILogger logger,
            IGetRepoPathValidity getRepoPathValidity,
            IProvideRepositoryCheckouts repoCheckouts,
            ISolutionFileLocator solutionFileLocator,
            ICheckOrCloneRepo checkOrClone,
            IDriverRepoDirectoryProvider driverRepoDirectoryProvider,
            ISchedulerProvider schedulerProvider)
        {
            _logger = logger;
            _getRepoPathValidity = getRepoPathValidity;
            _repoCheckouts = repoCheckouts;
            _solutionFileLocator = solutionFileLocator;
            _checkOrClone = checkOrClone;
            _driverRepoDirectoryProvider = driverRepoDirectoryProvider;
            _schedulerProvider = schedulerProvider;
            
            // Clone repository to a folder where driving information will be retrieved from master.
            // This will be where we get available projects + tags, etc.
            DriverInfo = _getRepoPathValidity.RepoPath
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
                        var state = _checkOrClone.Check(
                            path.ToGetResponse(), 
                            driverRepoPath,
                            cancel);
                        if (state.Failed)
                        {
                            _logger.Error("Failed to check out driver repository: {Reason}", state.Reason);
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
                                _logger.Error("Failed to check out driver repository: Could not locate master branch");
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
                            _logger.Error(ex, "Failed to check out driver repository");
                            return new ConfigurationState<DriverRepoInfo>(default!, ErrorResponse.Fail(ex));
                        }

                        // Try to locate a solution to drive from
                        var slnPath = _solutionFileLocator.GetPath(driverRepoPath);
                        if (slnPath == null)
                        {
                            _logger.Error("Failed to check out driver repository: Could not locate solution to run");
                            return new ConfigurationState<DriverRepoInfo>(default!,
                                ErrorResponse.Fail("Could not locate solution to run."));
                        }

                        var availableProjs = SolutionPatcherRun.AvailableProjectSubpaths(slnPath).ToList();
                        return new ConfigurationState<DriverRepoInfo>(
                            new DriverRepoInfo(
                                slnPath: slnPath.Value,
                                masterBranchName: masterBranch,
                                branchShas: branchShas,
                                tags: tags,
                                availableProjects: availableProjs));
                    })
                .Replay(1)
                .RefCount();
        }
    }
}