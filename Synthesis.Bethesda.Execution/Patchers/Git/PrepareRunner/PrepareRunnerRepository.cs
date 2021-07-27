using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Noggog;
using Serilog;
using Synthesis.Bethesda.Execution.DotNet;
using Synthesis.Bethesda.Execution.GitRepository;
using Synthesis.Bethesda.Execution.Patchers.Git.ModifyProject;
using Synthesis.Bethesda.Execution.Patchers.Solution;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.Execution.Patchers.Git.PrepareRunner
{
    public interface IPrepareRunnerRepository
    {
        Task<ConfigurationState<RunnerRepoInfo>> Checkout(
            CheckoutInput checkoutInput,
            CancellationToken cancel);
    }

    public class PrepareRunnerRepository : IPrepareRunnerRepository
    {
        private readonly ILogger _logger;
        private readonly ICheckoutRunnerBranch _checkoutRunnerBranch;
        private readonly ISolutionFileLocator _solutionFileLocator;
        private readonly IFullProjectPathRetriever _fullProjectPathRetriever;
        private readonly IModifyRunnerProjects _modifyRunnerProjects;
        private readonly IGetRepoTarget _getRepoTarget;
        private readonly IRetrieveCommit _retrieveCommit;
        private readonly IRunnerRepoDirectoryProvider _runnerRepoDirectoryProvider;
        public IProvideRepositoryCheckouts RepoCheckouts { get; }

        public PrepareRunnerRepository(
            ILogger logger,
            ICheckoutRunnerBranch checkoutRunnerBranch,
            ISolutionFileLocator solutionFileLocator,
            IFullProjectPathRetriever fullProjectPathRetriever,
            IModifyRunnerProjects modifyRunnerProjects,
            IGetRepoTarget getRepoTarget,
            IRetrieveCommit retrieveCommit,
            IRunnerRepoDirectoryProvider runnerRepoDirectoryProvider,
            IProvideRepositoryCheckouts repoCheckouts)
        {
            _logger = logger;
            _checkoutRunnerBranch = checkoutRunnerBranch;
            _solutionFileLocator = solutionFileLocator;
            _fullProjectPathRetriever = fullProjectPathRetriever;
            _modifyRunnerProjects = modifyRunnerProjects;
            _getRepoTarget = getRepoTarget;
            _retrieveCommit = retrieveCommit;
            _runnerRepoDirectoryProvider = runnerRepoDirectoryProvider;
            RepoCheckouts = repoCheckouts;
        }
        
        public async Task<ConfigurationState<RunnerRepoInfo>> Checkout(
            CheckoutInput checkoutInput,
            CancellationToken cancel)
        {
            try
            {
                var localRepoDir = _runnerRepoDirectoryProvider.Path;
                
                cancel.ThrowIfCancellationRequested();

                _logger.Information("Targeting {PatcherVersioning}", checkoutInput.PatcherVersioning);

                using var repoCheckout = RepoCheckouts.Get(localRepoDir);
                var repo = repoCheckout.Repository;
                
                _checkoutRunnerBranch.Checkout(repo);
                
                var targets = _getRepoTarget.Get(
                    repo, 
                    checkoutInput.PatcherVersioning);
                if (targets.Failed) return targets.BubbleFailure<RunnerRepoInfo>();

                var commit = _retrieveCommit.TryGet(
                    repo,
                    targets.Value,
                    checkoutInput.PatcherVersioning,
                    cancel);
                if (commit.Failed) return commit.BubbleFailure<RunnerRepoInfo>();

                cancel.ThrowIfCancellationRequested();
                
                var slnPath = _solutionFileLocator.GetPath(localRepoDir);
                if (slnPath == null) return GetResponse<RunnerRepoInfo>.Fail("Could not locate solution to run.");

                var foundProjSubPath = _fullProjectPathRetriever.Get(slnPath, checkoutInput.Proj);
                if (foundProjSubPath == null) return GetResponse<RunnerRepoInfo>.Fail($"Could not locate target project file: {checkoutInput.Proj}.");

                cancel.ThrowIfCancellationRequested();
                
                _logger.Information("Checking out {TargetSha}", targets.Value.TargetSha);
                repo.ResetHard(commit.Value);

                var projPath = Path.Combine(localRepoDir, foundProjSubPath);

                cancel.ThrowIfCancellationRequested();

                var nugetVersioning = checkoutInput.LibraryNugets;
                nugetVersioning.Log(_logger);
                
                _modifyRunnerProjects.Modify(
                    slnPath,
                    drivingProjSubPath: foundProjSubPath,
                    mutagenVersion: nugetVersioning.MutagenVersioning == NugetVersioningEnum.Match ? null : nugetVersioning.MutagenVersion,
                    listedMutagenVersion: out var listedMutagenVersion,
                    synthesisVersion: nugetVersioning.SynthesisVersioning == NugetVersioningEnum.Match ? null : nugetVersioning.SynthesisVersion,
                    listedSynthesisVersion: out var listedSynthesisVersion);

                var runInfo = new RunnerRepoInfo(
                    SolutionPath: slnPath,
                    ProjPath: projPath,
                    Target: targets.Value.Target,
                    CommitMessage: commit.Value.CommitMessage,
                    CommitDate: commit.Value.CommitDate,
                    ListedSynthesisVersion: listedSynthesisVersion,
                    ListedMutagenVersion: listedMutagenVersion,
                    TargetSynthesisVersion: nugetVersioning.SynthesisVersioning == NugetVersioningEnum.Match ? listedSynthesisVersion : nugetVersioning.SynthesisVersion,
                    TargetMutagenVersion: nugetVersioning.MutagenVersioning == NugetVersioningEnum.Match ? listedMutagenVersion : nugetVersioning.MutagenVersion);

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
}