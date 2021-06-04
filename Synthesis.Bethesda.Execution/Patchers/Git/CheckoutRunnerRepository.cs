using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LibGit2Sharp;
using Noggog;
using Synthesis.Bethesda.Execution.GitRespository;
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
        private readonly IProvideRepositoryCheckouts _RepoCheckouts;
        public const string RunnerBranch = "SynthesisRunner";

        public CheckoutRunnerRepository(IProvideRepositoryCheckouts repoCheckouts)
        {
            _RepoCheckouts = repoCheckouts;
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

                using var repoCheckout = _RepoCheckouts.Get(localRepoDir);
                var repo = repoCheckout.Repository;
                var runnerBranch = repo.Branches[RunnerBranch] ?? repo.CreateBranch(RunnerBranch);
                repo.Reset(ResetMode.Hard);
                Commands.Checkout(repo, runnerBranch);
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
                        targetSha = repo.Tags[patcherVersioning.Target]?.Target.Sha;
                        if (string.IsNullOrWhiteSpace(targetSha)) return GetResponse<RunnerRepoInfo>.Fail("Could not locate tag");
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
                        var targetBranch = repo.Branches[patcherVersioning.Target];
                        if (targetBranch == null) return GetResponse<RunnerRepoInfo>.Fail($"Could not locate branch: {patcherVersioning.Target}");
                        targetSha = targetBranch.Tip.Sha;
                        target = patcherVersioning.Target;
                        break;
                    default:
                        throw new NotImplementedException();
                }
                if (!ObjectId.TryParse(targetSha, out var objId)) return GetResponse<RunnerRepoInfo>.Fail("Malformed sha string");

                cancel.ThrowIfCancellationRequested();
                var commit = repo.Lookup(objId, ObjectType.Commit) as Commit;
                if (commit == null)
                {
                    if (!fetchIfMissing)
                    {
                        return GetResponse<RunnerRepoInfo>.Fail("Could not locate commit with given sha");
                    }
                    repo.Fetch();
                    commit = repo.Lookup(objId, ObjectType.Commit) as Commit;
                    if (commit == null)
                    {
                        return GetResponse<RunnerRepoInfo>.Fail("Could not locate commit with given sha");
                    }
                }

                cancel.ThrowIfCancellationRequested();
                var slnPath = GitPatcherRun.GetPathToSolution(localRepoDir);
                if (slnPath == null) return GetResponse<RunnerRepoInfo>.Fail("Could not locate solution to run.");

                var foundProjSubPath = SolutionPatcherRun.AvailableProject(slnPath, proj);

                if (foundProjSubPath == null) return GetResponse<RunnerRepoInfo>.Fail($"Could not locate target project file: {proj}.");

                cancel.ThrowIfCancellationRequested();
                logger?.Invoke($"Checking out {targetSha}");
                repo.Reset(ResetMode.Hard, commit, new CheckoutOptions());

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
                    var compileResp = await DotNetCommands.Compile(projPath, cancel, logger);
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