using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Noggog;
using Serilog;
using Synthesis.Bethesda.Execution.GitRepository;
using Synthesis.Bethesda.Execution.Patchers.Running;
using Synthesis.Bethesda.Execution.Patchers.Solution;

namespace Synthesis.Bethesda.Execution.Patchers.Git.PrepareDriver
{
    public interface IPrepareDriverRespository
    {
        GetResponse<DriverRepoInfo> Prepare(GetResponse<string> path, CancellationToken cancel);
    }

    public class PrepareDriverRespository : IPrepareDriverRespository
    {
        private readonly ILogger _logger;
        private readonly ICheckOrCloneRepo _checkOrClone;
        private readonly IProvideRepositoryCheckouts _repoCheckouts;
        private readonly ISolutionFileLocator _solutionFileLocator;
        private readonly IAvailableProjectsRetriever _availableProjectsRetriever;
        private readonly IDriverRepoDirectoryProvider _driverRepoDirectoryProvider;

        public PrepareDriverRespository(
            ILogger logger,
            ICheckOrCloneRepo checkOrClone,
            IProvideRepositoryCheckouts repoCheckouts,
            ISolutionFileLocator solutionFileLocator,
            IAvailableProjectsRetriever availableProjectsRetriever,
            IDriverRepoDirectoryProvider driverRepoDirectoryProvider)
        {
            _logger = logger;
            _checkOrClone = checkOrClone;
            _repoCheckouts = repoCheckouts;
            _solutionFileLocator = solutionFileLocator;
            _availableProjectsRetriever = availableProjectsRetriever;
            _driverRepoDirectoryProvider = driverRepoDirectoryProvider;
        }

        public GetResponse<DriverRepoInfo> Prepare(GetResponse<string> path, CancellationToken cancel)
        {
            var driverRepoPath = _driverRepoDirectoryProvider.Path;

            // Clone and/or double check the clone is correct
            var state = _checkOrClone.Check(
                path,
                driverRepoPath,
                cancel);
            if (state.Failed)
            {
                _logger.Error("Failed to check out driver repository: {Reason}", state.Reason);
                return state.BubbleFailure<DriverRepoInfo>();
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
                    return GetResponse<DriverRepoInfo>.Fail("Could not locate master branch.");
                }

                masterBranch = master.FriendlyName;
                repo.ResetHard();
                repo.Checkout(master);
                repo.Pull();
                tags = repo.Tags.Select(tag => (tag.FriendlyName, tag.Sha))
                    .WithIndex()
                    .Select(x => (x.Index, x.Item.FriendlyName, x.Item.Sha))
                    .ToList();
                branchShas = repo.Branches
                    .ToDictionary(x => x.FriendlyName, x => x.Tip.Sha, StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to check out driver repository");
                return GetResponse<DriverRepoInfo>.Fail(ex);
            }

            // Try to locate a solution to drive from
            var slnPath = _solutionFileLocator.GetPath(driverRepoPath);
            if (slnPath == null)
            {
                _logger.Error("Failed to check out driver repository: Could not locate solution to run");
                return GetResponse<DriverRepoInfo>.Fail("Could not locate solution to run.");
            }

            var availableProjs = _availableProjectsRetriever.Get(slnPath.Value).ToList();
            
            return new DriverRepoInfo(
                slnPath: slnPath.Value,
                masterBranchName: masterBranch,
                branchShas: branchShas,
                tags: tags,
                availableProjects: availableProjs);
        }
    }
}