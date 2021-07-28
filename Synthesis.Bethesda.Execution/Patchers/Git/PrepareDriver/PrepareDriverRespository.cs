using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Noggog;
using Serilog;
using Synthesis.Bethesda.Execution.GitRepository;
using Synthesis.Bethesda.Execution.Patchers.Solution;

namespace Synthesis.Bethesda.Execution.Patchers.Git.PrepareDriver
{
    public interface IPrepareDriverRespository
    {
        GetResponse<DriverRepoInfo> Prepare(GetResponse<string> remotePath, CancellationToken cancel);
    }

    public class PrepareDriverRespository : IPrepareDriverRespository
    {
        private readonly ILogger _logger;
        public ICheckOrCloneRepo CheckOrClone { get; }
        public IGetToLatestMaster GetToLatestMaster { get; }
        public IProvideRepositoryCheckouts RepoCheckouts { get; }
        public IGetDriverPaths GetDriverPaths { get; }
        public IRetrieveRepoVersioningPoints RetrieveRepoVersioningPoints { get; }
        public IDriverRepoDirectoryProvider DriverRepoDirectoryProvider { get; }

        public PrepareDriverRespository(
            ILogger logger,
            ICheckOrCloneRepo checkOrClone,
            IGetToLatestMaster getToLatestMaster,
            IProvideRepositoryCheckouts repoCheckouts,
            IGetDriverPaths getDriverPaths,
            IRetrieveRepoVersioningPoints retrieveRepoVersioningPoints,
            IDriverRepoDirectoryProvider driverRepoDirectoryProvider)
        {
            _logger = logger;
            CheckOrClone = checkOrClone;
            GetToLatestMaster = getToLatestMaster;
            RepoCheckouts = repoCheckouts;
            GetDriverPaths = getDriverPaths;
            RetrieveRepoVersioningPoints = retrieveRepoVersioningPoints;
            DriverRepoDirectoryProvider = driverRepoDirectoryProvider;
        }

        public GetResponse<DriverRepoInfo> Prepare(GetResponse<string> remotePath, CancellationToken cancel)
        {
            // Clone and/or double check the clone is correct
            var state = CheckOrClone.Check(
                remotePath,
                DriverRepoDirectoryProvider.Path,
                cancel);
            if (state.Failed)
            {
                _logger.Error("Failed to check out driver repository: {Reason}", state.Reason);
                return state.BubbleFailure<DriverRepoInfo>();
            }

            cancel.ThrowIfCancellationRequested();

            // Grab all the interesting metadata
            List<DriverTag> tags;
            Dictionary<string, string> branchShas;
            string masterBranch;
            try
            {
                using var repoCheckout = RepoCheckouts.Get(DriverRepoDirectoryProvider.Path);

                if (!GetToLatestMaster.TryGet(repoCheckout.Repository, out masterBranch!))
                {
                    _logger.Error("Failed to check out driver repository: Could not locate master branch");
                    return GetResponse<DriverRepoInfo>.Fail("Could not locate master branch.");
                }
                
                RetrieveRepoVersioningPoints.Retrieve(repoCheckout.Repository, out tags, out branchShas);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to check out driver repository");
                return GetResponse<DriverRepoInfo>.Fail(ex);
            }

            var paths = GetDriverPaths.Get();
            if (paths.Failed)
            {
                _logger.Error("Failed to check out driver repository: {Reason}", paths.Reason);
                return paths.BubbleFailure<DriverRepoInfo>();
            }
            
            return new DriverRepoInfo(
                SolutionPath: paths.Value.SolutionPath,
                MasterBranchName: masterBranch,
                BranchShas: branchShas,
                Tags: tags,
                AvailableProjects: paths.Value.AvailableProjects);
        }
    }
}