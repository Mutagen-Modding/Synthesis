using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Noggog;
using Serilog;
using Synthesis.Bethesda.Execution.DotNet;
using Synthesis.Bethesda.Execution.GitRepository;
using Synthesis.Bethesda.Execution.Patchers.Git.ModifyProject;
using Synthesis.Bethesda.Execution.Patchers.Running;
using Synthesis.Bethesda.Execution.Patchers.Solution;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.Execution.Patchers.Git.CheckoutRunner
{
    public interface ICheckoutRunnerRepository
    {
        Task<ConfigurationState<RunnerRepoInfo>> Checkout(
            string proj,
            DirectoryPath localRepoDir,
            GitPatcherVersioning patcherVersioning,
            NugetVersioningTarget nugetVersioning,
            CancellationToken cancel,
            bool compile = true);
    }

    public class CheckoutRunnerRepository : ICheckoutRunnerRepository
    {
        private readonly ILogger _logger;
        private readonly IBuild _build;
        private readonly ISolutionFileLocator _solutionFileLocator;
        private readonly IFullProjectPathRetriever _fullProjectPathRetriever;
        private readonly IModifyRunnerProjects _modifyRunnerProjects;
        public IProvideRepositoryCheckouts RepoCheckouts { get; }
        public const string RunnerBranch = "SynthesisRunner";

        public CheckoutRunnerRepository(
            ILogger logger,
            IBuild build,
            ISolutionFileLocator solutionFileLocator,
            IFullProjectPathRetriever fullProjectPathRetriever,
            IModifyRunnerProjects modifyRunnerProjects,
            IProvideRepositoryCheckouts repoCheckouts)
        {
            _logger = logger;
            _build = build;
            _solutionFileLocator = solutionFileLocator;
            _fullProjectPathRetriever = fullProjectPathRetriever;
            _modifyRunnerProjects = modifyRunnerProjects;
            RepoCheckouts = repoCheckouts;
        }
        
        public async Task<ConfigurationState<RunnerRepoInfo>> Checkout(
            string proj,
            DirectoryPath localRepoDir,
            GitPatcherVersioning patcherVersioning,
            NugetVersioningTarget nugetVersioning,
            CancellationToken cancel,
            bool compile = true)
        {
            try
            {
                cancel.ThrowIfCancellationRequested();

                _logger.Information("Targeting {PatcherVersioning}", patcherVersioning);

                using var repoCheckout = RepoCheckouts.Get(localRepoDir);
                var repo = repoCheckout.Repository;
                var runnerBranch = repo.TryCreateBranch(RunnerBranch);
                repo.ResetHard();
                repo.Checkout(runnerBranch);
                string? targetSha;
                string? target;
                bool fetchIfMissing = patcherVersioning.Versioning switch
                {
                    PatcherVersioningEnum.Commit => true,
                    _ => false
                };
                switch (patcherVersioning.Versioning)
                {
                    case PatcherVersioningEnum.Tag:
                        if (string.IsNullOrWhiteSpace(patcherVersioning.Target)) return GetResponse<RunnerRepoInfo>.Fail("No tag selected");
                        repo.Fetch();
                        if (!repo.TryGetTagSha(patcherVersioning.Target, out targetSha)) return GetResponse<RunnerRepoInfo>.Fail("Could not locate tag");
                        target = patcherVersioning.Target;
                        break;
                    case PatcherVersioningEnum.Commit:
                        targetSha = patcherVersioning.Target;
                        if (string.IsNullOrWhiteSpace(targetSha)) return GetResponse<RunnerRepoInfo>.Fail("Could not locate commit");
                        target = patcherVersioning.Target;
                        break;
                    case PatcherVersioningEnum.Branch:
                        if (string.IsNullOrWhiteSpace(patcherVersioning.Target)) return GetResponse<RunnerRepoInfo>.Fail($"Target branch had no name.");
                        repo.Fetch();
                        if (!repo.TryGetBranch(patcherVersioning.Target, out var targetBranch)) return GetResponse<RunnerRepoInfo>.Fail($"Could not locate branch: {patcherVersioning.Target}");
                        targetSha = targetBranch.Tip.Sha;
                        target = patcherVersioning.Target;
                        break;
                    default:
                        throw new NotImplementedException();
                }

                var commit = repo.TryGetCommit(targetSha, out var validSha);
                if (!validSha)
                {
                    return GetResponse<RunnerRepoInfo>.Fail("Malformed sha string");
                }

                cancel.ThrowIfCancellationRequested();
                if (commit == null)
                {
                    if (!fetchIfMissing)
                    {
                        return GetResponse<RunnerRepoInfo>.Fail("Could not locate commit with given sha");
                    }
                    repo.Fetch();
                    commit = repo.TryGetCommit(targetSha, out _);
                    if (commit == null)
                    {
                        return GetResponse<RunnerRepoInfo>.Fail("Could not locate commit with given sha");
                    }
                }

                cancel.ThrowIfCancellationRequested();
                var slnPath = _solutionFileLocator.GetPath(localRepoDir);
                if (slnPath == null) return GetResponse<RunnerRepoInfo>.Fail("Could not locate solution to run.");

                var foundProjSubPath = _fullProjectPathRetriever.Get(slnPath, proj);

                if (foundProjSubPath == null) return GetResponse<RunnerRepoInfo>.Fail($"Could not locate target project file: {proj}.");

                cancel.ThrowIfCancellationRequested();
                _logger.Information("Checking out {TargetSha}", targetSha);
                repo.ResetHard(commit);

                var projPath = Path.Combine(localRepoDir, foundProjSubPath);

                cancel.ThrowIfCancellationRequested();
                _logger.Information("Mutagen Nuget: {Versioning} {Version}", nugetVersioning.MutagenVersioning, nugetVersioning.MutagenVersion);
                _logger.Information("Synthesis Nuget: {Versioning} {Version}", nugetVersioning.SynthesisVersioning, nugetVersioning.SynthesisVersion);
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
                    Target: target,
                    CommitMessage: commit.Message,
                    CommitDate: commit.Author.When.LocalDateTime,
                    ListedSynthesisVersion: listedSynthesisVersion,
                    ListedMutagenVersion: listedMutagenVersion,
                    TargetSynthesisVersion: nugetVersioning.SynthesisVersioning == NugetVersioningEnum.Match ? listedSynthesisVersion : nugetVersioning.SynthesisVersion,
                    TargetMutagenVersion: nugetVersioning.MutagenVersioning == NugetVersioningEnum.Match ? listedMutagenVersion : nugetVersioning.MutagenVersion);

                // Compile to help prep
                if (compile)
                {
                    var compileResp = await _build.Compile(projPath, cancel);
                    _logger.Information("Finished compiling");
                    if (compileResp.Failed) return compileResp.BubbleResult(runInfo);
                }

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