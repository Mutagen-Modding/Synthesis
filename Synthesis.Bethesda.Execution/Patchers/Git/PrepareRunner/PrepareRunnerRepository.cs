using Noggog;
using Serilog;
using Synthesis.Bethesda.Execution.GitRepository;
using Synthesis.Bethesda.Execution.Patchers.Git.ModifyProject;
using Synthesis.Bethesda.Execution.Patchers.Solution;
using Synthesis.Bethesda.Execution.Versioning;

namespace Synthesis.Bethesda.Execution.Patchers.Git.PrepareRunner;

public interface IPrepareRunnerRepository
{
    Task<ConfigurationState<RunnerRepoInfo>> Checkout(
        CheckoutInput checkoutInput,
        CancellationToken cancel);
}

public class PrepareRunnerRepository : IPrepareRunnerRepository
{
    private readonly ILogger _logger;
    private readonly IBuildMetaFilePathProvider _metaFilePathProvider;
    public IResetToTarget ResetToTarget { get; }
    public ISolutionFileLocator SolutionFileLocator { get; }
    public IRunnerRepoProjectPathRetriever RunnerRepoProjectPathRetriever { get; }
    public IModifyRunnerProjects ModifyRunnerProjects { get; }
    public IRunnerRepoDirectoryProvider RunnerRepoDirectoryProvider { get; }
    public IProvideRepositoryCheckouts RepoCheckouts { get; }

    public PrepareRunnerRepository(
        ILogger logger,
        ISolutionFileLocator solutionFileLocator,
        IRunnerRepoProjectPathRetriever runnerRepoProjectPathRetriever,
        IModifyRunnerProjects modifyRunnerProjects,
        IResetToTarget resetToTarget,
        IBuildMetaFilePathProvider metaFilePathProvider,
        IRunnerRepoDirectoryProvider runnerRepoDirectoryProvider,
        IProvideRepositoryCheckouts repoCheckouts)
    {
        _logger = logger;
        _metaFilePathProvider = metaFilePathProvider;
        ResetToTarget = resetToTarget;
        SolutionFileLocator = solutionFileLocator;
        RunnerRepoProjectPathRetriever = runnerRepoProjectPathRetriever;
        ModifyRunnerProjects = modifyRunnerProjects;
        RunnerRepoDirectoryProvider = runnerRepoDirectoryProvider;
        RepoCheckouts = repoCheckouts;
    }
        
    public async Task<ConfigurationState<RunnerRepoInfo>> Checkout(
        CheckoutInput checkoutInput,
        CancellationToken cancel)
    {
        try
        {
            cancel.ThrowIfCancellationRequested();

            _logger.Information("Targeting {PatcherVersioning}", checkoutInput.PatcherVersioning);

            using var repoCheckout = RepoCheckouts.Get(RunnerRepoDirectoryProvider.Path);

            var target = ResetToTarget.Reset(repoCheckout.Repository, checkoutInput.PatcherVersioning, cancel);
            if (target.Failed) return target.BubbleFailure<RunnerRepoInfo>();

            cancel.ThrowIfCancellationRequested();

            checkoutInput.LibraryNugets.Log(_logger);
                
            var slnPath = SolutionFileLocator.GetPath(RunnerRepoDirectoryProvider.Path);
            if (slnPath == null) return GetResponse<RunnerRepoInfo>.Fail("Could not locate solution to run.");

            var foundProjSubPath = RunnerRepoProjectPathRetriever.Get(slnPath.Value, checkoutInput.Proj);
            if (foundProjSubPath == null) return GetResponse<RunnerRepoInfo>.Fail($"Could not locate target project file: {checkoutInput.Proj}.");

            cancel.ThrowIfCancellationRequested();
                
            ModifyRunnerProjects.Modify(
                slnPath.Value,
                drivingProjSubPath: foundProjSubPath.SubPath,
                versions: checkoutInput.LibraryNugets.ReturnIfMatch(new NugetVersionPair(null, null)),
                listedVersions: out var listedVersions);

            var runInfo = new RunnerRepoInfo(
                Project: new TargetProject(
                    SolutionPath: slnPath.Value,
                    ProjPath: foundProjSubPath.FullPath,
                    ProjSubPath: foundProjSubPath.SubPath),
                MetaPath: _metaFilePathProvider.Path,
                Target: target.Value.Target,
                CommitMessage: target.Value.CommitMessage,
                CommitDate: target.Value.CommitDate,
                ListedVersions: listedVersions,
                TargetVersions: checkoutInput.LibraryNugets.ReturnIfMatch(listedVersions));

            return GetResponse<RunnerRepoInfo>.Succeed(runInfo);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return GetResponse<RunnerRepoInfo>.Fail(ex);
        }
    }
}