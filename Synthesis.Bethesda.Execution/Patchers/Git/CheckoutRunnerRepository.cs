using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LibGit2Sharp;
using Noggog;
using Synthesis.Bethesda.Execution.DotNet;
using Synthesis.Bethesda.Execution.GitRespository;
using Synthesis.Bethesda.Execution.Patchers.Running;
using Synthesis.Bethesda.Execution.Patchers.Solution;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.Execution.Patchers.Git
{
    public interface ICheckoutRunnerRepository
    {
        Task<ConfigurationState<RunnerRepoInfo>> Checkout(
            string proj,
            string localRepoDir,
            GitPatcherVersioning patcherVersioning,
            NugetVersioningTarget nugetVersioning,
            Action<string>? logger,
            CancellationToken cancel,
            bool compile = true);
    }

    public class CheckoutRunnerRepository : ICheckoutRunnerRepository
    {
        private readonly IBuild _Build;
        private readonly IPathToSolutionProvider _pathToSolutionProvider;
        public IProvideRepositoryCheckouts RepoCheckouts { get; }
        public const string RunnerBranch = "SynthesisRunner";

        public CheckoutRunnerRepository(
            IBuild build,
            IPathToSolutionProvider _pathToSolutionProvider,
            IProvideRepositoryCheckouts repoCheckouts)
        {
            _Build = build;
            this._pathToSolutionProvider = _pathToSolutionProvider;
            RepoCheckouts = repoCheckouts;
        }
        
        public async Task<ConfigurationState<RunnerRepoInfo>> Checkout(
            string proj,
            string localRepoDir,
            GitPatcherVersioning patcherVersioning,
            NugetVersioningTarget nugetVersioning,
            Action<string>? logger,
            CancellationToken cancel,
            bool compile = true)
        {
            try
            {
                cancel.ThrowIfCancellationRequested();

                logger?.Invoke($"Targeting {patcherVersioning}");

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
                var slnPath = _pathToSolutionProvider.Path;
                if (slnPath == null) return GetResponse<RunnerRepoInfo>.Fail("Could not locate solution to run.");

                var foundProjSubPath = SolutionPatcherRun.AvailableProject(slnPath, proj);

                if (foundProjSubPath == null) return GetResponse<RunnerRepoInfo>.Fail($"Could not locate target project file: {proj}.");

                cancel.ThrowIfCancellationRequested();
                logger?.Invoke($"Checking out {targetSha}");
                repo.ResetHard(commit);

                var projPath = Path.Combine(localRepoDir, foundProjSubPath);

                cancel.ThrowIfCancellationRequested();
                logger?.Invoke($"Mutagen Nuget: {nugetVersioning.MutagenVersioning} {nugetVersioning.MutagenVersion}");
                logger?.Invoke($"Synthesis Nuget: {nugetVersioning.SynthesisVersioning} {nugetVersioning.SynthesisVersion}");
                GitPatcherRun.ModifyProject(
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
                    var compileResp = await _Build.Compile(projPath, cancel, logger);
                    logger?.Invoke("Finished compiling");
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