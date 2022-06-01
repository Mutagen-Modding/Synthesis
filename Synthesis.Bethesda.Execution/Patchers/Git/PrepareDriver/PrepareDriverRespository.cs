using Noggog;
using Serilog;
using Synthesis.Bethesda.Execution.GitRepository;

namespace Synthesis.Bethesda.Execution.Patchers.Git.PrepareDriver;

public interface IPrepareDriverRespository
{
    GetResponse<DriverRepoInfo> Prepare(GetResponse<string> remotePath, CancellationToken cancel);
}

public class PrepareDriverRespository : IPrepareDriverRespository
{
    private readonly ILogger _logger;
    public ICheckOrCloneRepo CheckOrClone { get; }
    public IResetToLatestMain ResetToLatestMain { get; }
    public IProvideRepositoryCheckouts RepoCheckouts { get; }
    public IGetDriverPaths GetDriverPaths { get; }
    public IRetrieveRepoVersioningPoints RetrieveRepoVersioningPoints { get; }
    public IDriverRepoDirectoryProvider DriverRepoDirectoryProvider { get; }

    public PrepareDriverRespository(
        ILogger logger,
        ICheckOrCloneRepo checkOrClone,
        IResetToLatestMain resetToLatestMain,
        IProvideRepositoryCheckouts repoCheckouts,
        IGetDriverPaths getDriverPaths,
        IRetrieveRepoVersioningPoints retrieveRepoVersioningPoints,
        IDriverRepoDirectoryProvider driverRepoDirectoryProvider)
    {
        _logger = logger;
        CheckOrClone = checkOrClone;
        ResetToLatestMain = resetToLatestMain;
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
            _logger.Error("Failed to check out driver repository because the remote path was not supplied: {Reason}", state.Reason);
            return state.BubbleFailure<DriverRepoInfo>();
        }

        cancel.ThrowIfCancellationRequested();

        // Grab all the interesting metadata
        List<DriverTag> tags;
        Dictionary<string, string> branchShas;
        IBranch masterBranch;
        try
        {
            using var repoCheckout = RepoCheckouts.Get(DriverRepoDirectoryProvider.Path);

            var masterBranchGet = ResetToLatestMain.TryReset(repoCheckout.Repository);
            if (masterBranchGet.Failed)
            {
                _logger.Error("Failed to check out driver repository because the master branch was unable to be retrieved {Repo}: {Reason}", remotePath.Value, masterBranchGet.Reason);
                return masterBranchGet.BubbleFailure<DriverRepoInfo>();
            }

            masterBranch = masterBranchGet.Value;
                
            RetrieveRepoVersioningPoints.Retrieve(repoCheckout.Repository, out tags, out branchShas);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to check out driver repository due to unexpected exception {Repo}", remotePath.Value);
            return GetResponse<DriverRepoInfo>.Fail(ex);
        }

        var paths = GetDriverPaths.Get();
        if (paths.Failed)
        {
            _logger.Error("Failed to check out driver repository because driver paths could not be retrieved {Repo}: {Reason}", remotePath.Value, paths.Reason);
            return paths.BubbleFailure<DriverRepoInfo>();
        }
            
        return new DriverRepoInfo(
            SolutionPath: paths.Value.SolutionPath,
            MasterBranchName: masterBranch.FriendlyName,
            BranchShas: branchShas,
            Tags: tags,
            AvailableProjects: paths.Value.AvailableProjects);
    }
}