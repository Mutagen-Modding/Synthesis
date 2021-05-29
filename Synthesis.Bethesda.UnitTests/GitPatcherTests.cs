using FluentAssertions;
using LibGit2Sharp;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Noggog;
using Noggog.Utility;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Settings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Synthesis.Bethesda.UnitTests
{
    public class GitPatcherTests
    {
        public TempFolder GetRepository(
            out string remote, 
            out string local,
            bool createPatcherFiles = true)
        {
            var folder = Utility.GetTempFolder(nameof(GitPatcherTests));

            local = Path.Combine(folder.Dir.Path, "Local");
            Repository.Init(local);
            remote = Path.Combine(folder.Dir.Path, "Remote");
            Repository.Init(remote, isBare: true);

            Directory.CreateDirectory(local);
            using var localRepo = new Repository(local);
            File.AppendAllText(Path.Combine(local, AFile), "Hello there");
            Commands.Stage(localRepo, AFile);
            var sig = Signature;
            localRepo.Commit("Initial commit", sig, sig);

            if (createPatcherFiles)
            {
                var files = SolutionInitialization.CreateSolutionFile(Path.Combine(local, SlnPath))
                    .And(SolutionInitialization.CreateProject(Path.Combine(local, ProjPath), GameCategory.Skyrim));
                SolutionInitialization.AddProjectToSolution(Path.Combine(local, SlnPath), Path.Combine(local, ProjPath));
                foreach (var path in files)
                {
                    Commands.Stage(localRepo, path);
                }
                localRepo.Commit("Added solution", sig, sig);
            }

            var remoteRef = localRepo.Network.Remotes.Add("origin", remote);
            var master = localRepo.Branches[DefaultBranch];
            localRepo.Branches.Update(
                master, 
                b => b.Remote = remoteRef.Name, 
                b => b.UpstreamBranch = master.CanonicalName);
            localRepo.Network.Push(master);

            return folder;
        }

        public Commit AddACommit(string path)
        {
            File.AppendAllText(Path.Combine(path, AFile), "Hello there");
            using var repo = new Repository(path);
            Commands.Stage(repo, AFile);
            var sig = Signature;
            var commit = repo.Commit("A commit", sig, sig);
            repo.Network.Push(repo.Head);
            return commit;
        }

        public string DefaultBranch => "master";
        public string AFile => "Somefile.txt";
        public string SlnPath => "Solution.sln";
        public string ProjPath => "MyProj/MyProj.csproj";
        public Signature Signature => new("noggog", "someEmail@gmail.com", DateTimeOffset.Now);

        [DebuggerStepThrough]
        public GitPatcherVersioning TypicalPatcherVersioning() =>
            new(
                PatcherVersioningEnum.Branch,
                target: DefaultBranch);

        [DebuggerStepThrough]
        public NugetVersioningTarget TypicalNugetVersioning() =>
            new(
                mutagenVersion: null,
                mutagenVersioning: NugetVersioningEnum.Match,
                synthesisVersion: null,
                synthesisVersioning: NugetVersioningEnum.Match);

        #region Checkout Runner
        [Fact]
        public async Task ThrowsIfCancelled()
        {
            CancellationTokenSource cancel = new();
            cancel.Cancel();
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
            {
                return GitPatcherRun.CheckoutRunnerRepository(
                    proj: string.Empty,
                    localRepoDir: string.Empty,
                    patcherVersioning: null!,
                    nugetVersioning: null!,
                    logger: null,
                    cancel: cancel.Token,
                    compile: false);
            });
        }

        [Fact]
        public async Task CreatesRunnerBranch()
        {
            using var repoPath = GetRepository(out var remote, out var local);
            var resp = await GitPatcherRun.CheckoutRunnerRepository(
                proj: ProjPath,
                localRepoDir: local,
                patcherVersioning: TypicalPatcherVersioning(),
                nugetVersioning: TypicalNugetVersioning(),
                logger: null,
                cancel: CancellationToken.None,
                compile: false);
            using var repo = new Repository(local);
            repo.Branches.Select(x => x.FriendlyName).Should().Contain(GitPatcherRun.RunnerBranch);
        }

        [Fact]
        public async Task NonExistantProjPath()
        {
            using var repoPath = GetRepository(out var remote, out var local);
            var resp = await GitPatcherRun.CheckoutRunnerRepository(
                proj: string.Empty,
                localRepoDir: local,
                patcherVersioning: TypicalPatcherVersioning(),
                nugetVersioning: TypicalNugetVersioning(),
                logger: null,
                cancel: CancellationToken.None,
                compile: false);
            resp.IsHaltingError.Should().BeTrue();
            resp.RunnableState.Succeeded.Should().BeFalse();
            resp.RunnableState.Reason.Should().Contain("Could not locate target project file");
        }

        [Fact]
        public async Task NonExistantSlnPath()
        {
            using var repoPath = GetRepository(out var remote, out var local,
                createPatcherFiles: false);
            var resp = await GitPatcherRun.CheckoutRunnerRepository(
                proj: ProjPath,
                localRepoDir: local,
                patcherVersioning: TypicalPatcherVersioning(),
                nugetVersioning: TypicalNugetVersioning(),
                logger: null,
                cancel: CancellationToken.None,
                compile: false);
            resp.IsHaltingError.Should().BeTrue();
            resp.RunnableState.Succeeded.Should().BeFalse();
            resp.RunnableState.Reason.Should().Contain("Could not locate solution");
        }

        [Fact]
        public async Task UnknownSha()
        {
            using var repoPath = GetRepository(out var remote, out var local);
            var versioning = new GitPatcherVersioning(PatcherVersioningEnum.Commit, "46c207318c1531de7dc2f8e8c2a91aced183bc30");
            var resp = await GitPatcherRun.CheckoutRunnerRepository(
                proj: ProjPath,
                localRepoDir: local,
                patcherVersioning: versioning,
                nugetVersioning: TypicalNugetVersioning(),
                logger: null,
                cancel: CancellationToken.None,
                compile: false);
            resp.IsHaltingError.Should().BeTrue();
            resp.RunnableState.Succeeded.Should().BeFalse();
            resp.RunnableState.Reason.Should().Contain("Could not locate commit with given sha");
        }

        [Fact]
        public async Task MalformedSha()
        {
            using var repoPath = GetRepository(out var remote, out var local);
            var versioning = new GitPatcherVersioning(PatcherVersioningEnum.Commit, "derp");
            var resp = await GitPatcherRun.CheckoutRunnerRepository(
                proj: ProjPath,
                localRepoDir: local,
                patcherVersioning: versioning,
                nugetVersioning: TypicalNugetVersioning(),
                logger: null,
                cancel: CancellationToken.None,
                compile: false);
            resp.IsHaltingError.Should().BeTrue();
            resp.RunnableState.Succeeded.Should().BeFalse();
            resp.RunnableState.Reason.Should().Contain("Malformed sha string");
        }

        [Fact]
        public async Task TagTarget()
        {
            using var repoPath = GetRepository(out var remote, out var local);
            var tagStr = "1.3.4";
            using var repo = new Repository(local);
            var tipSha = repo.Head.Tip.Sha;
            repo.Tags.Add(tagStr, tipSha);
            var versioning = new GitPatcherVersioning(PatcherVersioningEnum.Tag, tagStr);
            var resp = await GitPatcherRun.CheckoutRunnerRepository(
                proj: ProjPath,
                localRepoDir: local,
                patcherVersioning: versioning,
                nugetVersioning: TypicalNugetVersioning(),
                logger: null,
                cancel: CancellationToken.None,
                compile: false);
            resp.IsHaltingError.Should().BeFalse();
            resp.RunnableState.Succeeded.Should().BeTrue();
            repo.Head.Tip.Sha.Should().BeEquivalentTo(tipSha);
            repo.Head.FriendlyName.Should().BeEquivalentTo(GitPatcherRun.RunnerBranch);
        }

        [Fact]
        public async Task TagTargetNoLocal()
        {
            using var repoPath = GetRepository(out var remote, out var local);
            var tagStr = "1.3.4";
            string tipSha;
            {
                using var repo = new Repository(local);
                tipSha = repo.Head.Tip.Sha;
                var tag = repo.Tags.Add(tagStr, tipSha);
                repo.Network.Push(repo.Network.Remotes.First(), tag.CanonicalName);

                AddACommit(local);
                repo.Head.Tip.Sha.Should().NotBeEquivalentTo(tipSha);
            }

            var clonePath = Path.Combine(repoPath.Dir.Path, "Clone");
            using var clone = new Repository(Repository.Clone(remote, clonePath));

            var versioning = new GitPatcherVersioning(PatcherVersioningEnum.Tag, tagStr);
            var resp = await GitPatcherRun.CheckoutRunnerRepository(
                proj: ProjPath,
                localRepoDir: clonePath,
                patcherVersioning: versioning,
                nugetVersioning: TypicalNugetVersioning(),
                logger: null,
                cancel: CancellationToken.None,
                compile: false);
            resp.IsHaltingError.Should().BeFalse();
            resp.RunnableState.Succeeded.Should().BeTrue();
            clone.Head.Tip.Sha.Should().BeEquivalentTo(tipSha);
            clone.Head.FriendlyName.Should().BeEquivalentTo(GitPatcherRun.RunnerBranch);
        }

        [Fact]
        public async Task TagTargetJumpback()
        {
            using var repoPath = GetRepository(out var remote, out var local);
            var tag = "1.3.4";
            using var repo = new Repository(local);
            var tipSha = repo.Head.Tip.Sha;
            repo.Tags.Add("1.3.4", tipSha);

            AddACommit(local);
            repo.Head.Tip.Sha.Should().NotBeEquivalentTo(tipSha);

            var versioning = new GitPatcherVersioning(PatcherVersioningEnum.Tag, tag);
            var resp = await GitPatcherRun.CheckoutRunnerRepository(
                proj: ProjPath,
                localRepoDir: local,
                patcherVersioning: versioning,
                nugetVersioning: TypicalNugetVersioning(),
                logger: null,
                cancel: CancellationToken.None,
                compile: false);
            resp.IsHaltingError.Should().BeFalse();
            resp.RunnableState.Succeeded.Should().BeTrue();
            repo.Head.Tip.Sha.Should().BeEquivalentTo(tipSha);
            repo.Head.FriendlyName.Should().BeEquivalentTo(GitPatcherRun.RunnerBranch);
        }

        [Fact]
        public async Task TagTargetNewContent()
        {
            using var repoPath = GetRepository(out var remote, out var local);

            var clonePath = Path.Combine(repoPath.Dir.Path, "Clone");
            using var clone = new Repository(Repository.Clone(remote, clonePath));

            var tagStr = "1.3.4";
            string commitSha;
            {
                using var repo = new Repository(local);
                AddACommit(local);
                commitSha = repo.Head.Tip.Sha;
                var tag = repo.Tags.Add(tagStr, commitSha);
                repo.Network.Push(repo.Network.Remotes.First(), tag.CanonicalName);
            }

            var versioning = new GitPatcherVersioning(PatcherVersioningEnum.Tag, tagStr);
            var resp = await GitPatcherRun.CheckoutRunnerRepository(
                proj: ProjPath,
                localRepoDir: clonePath,
                patcherVersioning: versioning,
                nugetVersioning: TypicalNugetVersioning(),
                logger: null,
                cancel: CancellationToken.None,
                compile: false);
            resp.IsHaltingError.Should().BeFalse();
            resp.RunnableState.Succeeded.Should().BeTrue();
            clone.Head.Tip.Sha.Should().BeEquivalentTo(commitSha);
            clone.Head.FriendlyName.Should().BeEquivalentTo(GitPatcherRun.RunnerBranch);
        }

        [Fact]
        public async Task CommitTargetJumpback()
        {
            using var repoPath = GetRepository(out var remote, out var local);
            using var repo = new Repository(local);
            var tipSha = repo.Head.Tip.Sha;

            AddACommit(local);
            repo.Head.Tip.Sha.Should().NotBeEquivalentTo(tipSha);

            var versioning = new GitPatcherVersioning(PatcherVersioningEnum.Commit, tipSha);
            var resp = await GitPatcherRun.CheckoutRunnerRepository(
                proj: ProjPath,
                localRepoDir: local,
                patcherVersioning: versioning,
                nugetVersioning: TypicalNugetVersioning(),
                logger: null,
                cancel: CancellationToken.None,
                compile: false);
            resp.IsHaltingError.Should().BeFalse();
            resp.RunnableState.Succeeded.Should().BeTrue();
            repo.Head.Tip.Sha.Should().BeEquivalentTo(tipSha);
            repo.Head.FriendlyName.Should().BeEquivalentTo(GitPatcherRun.RunnerBranch);
        }

        [Fact]
        public async Task CommitTargetNoLocal()
        {
            using var repoPath = GetRepository(out var remote, out var local);
            string tipSha;
            {
                using var repo = new Repository(local);
                tipSha = repo.Head.Tip.Sha;

                AddACommit(local);
                repo.Head.Tip.Sha.Should().NotBeEquivalentTo(tipSha);
            }

            var clonePath = Path.Combine(repoPath.Dir.Path, "Clone");
            using var clone = new Repository(Repository.Clone(remote, clonePath));

            var versioning = new GitPatcherVersioning(PatcherVersioningEnum.Commit, tipSha);
            var resp = await GitPatcherRun.CheckoutRunnerRepository(
                proj: ProjPath,
                localRepoDir: clonePath,
                patcherVersioning: versioning,
                nugetVersioning: TypicalNugetVersioning(),
                logger: null,
                cancel: CancellationToken.None,
                compile: false);
            resp.IsHaltingError.Should().BeFalse();
            resp.RunnableState.Succeeded.Should().BeTrue();
            clone.Head.Tip.Sha.Should().BeEquivalentTo(tipSha);
            clone.Head.FriendlyName.Should().BeEquivalentTo(GitPatcherRun.RunnerBranch);
        }

        [Fact]
        public async Task CommitTargetNewContent()
        {
            using var repoPath = GetRepository(out var remote, out var local);

            var clonePath = Path.Combine(repoPath.Dir.Path, "Clone");
            using var clone = new Repository(Repository.Clone(remote, clonePath));

            string tipSha, commitSha;
            {
                using var repo = new Repository(local);
                tipSha = repo.Head.Tip.Sha;

                commitSha = AddACommit(local).Sha;
                repo.Head.Tip.Sha.Should().NotBeEquivalentTo(tipSha);
            }

            var versioning = new GitPatcherVersioning(PatcherVersioningEnum.Commit, commitSha);
            var resp = await GitPatcherRun.CheckoutRunnerRepository(
                proj: ProjPath,
                localRepoDir: clonePath,
                patcherVersioning: versioning,
                nugetVersioning: TypicalNugetVersioning(),
                logger: null,
                cancel: CancellationToken.None,
                compile: false);
            resp.IsHaltingError.Should().BeFalse();
            resp.RunnableState.Succeeded.Should().BeTrue();
            clone.Head.Tip.Sha.Should().BeEquivalentTo(commitSha);
            clone.Head.FriendlyName.Should().BeEquivalentTo(GitPatcherRun.RunnerBranch);
        }

        [Fact]
        public async Task BranchTargetJumpback()
        {
            using var repoPath = GetRepository(out var remote, out var local);
            using var repo = new Repository(local);
            var tipSha = repo.Head.Tip.Sha;
            var jbName = "Jumpback";
            var jbBranch = repo.CreateBranch(jbName);
            repo.Branches.Update(
                jbBranch,
                b => b.Remote = repo.Network.Remotes.First().Name,
                b => b.UpstreamBranch = jbBranch.CanonicalName);
            repo.Network.Push(jbBranch);

            var commit = AddACommit(local);
            repo.Head.Tip.Sha.Should().NotBeEquivalentTo(tipSha);

            var versioning = new GitPatcherVersioning(PatcherVersioningEnum.Branch, jbName);
            var resp = await GitPatcherRun.CheckoutRunnerRepository(
                proj: ProjPath,
                localRepoDir: local,
                patcherVersioning: versioning,
                nugetVersioning: TypicalNugetVersioning(),
                logger: null,
                cancel: CancellationToken.None,
                compile: false);
            resp.IsHaltingError.Should().BeFalse();
            resp.RunnableState.Succeeded.Should().BeTrue();
            repo.Head.Tip.Sha.Should().BeEquivalentTo(tipSha);
            repo.Head.FriendlyName.Should().BeEquivalentTo(GitPatcherRun.RunnerBranch);
            repo.Branches[jbName].Tip.Sha.Should().BeEquivalentTo(tipSha);
            repo.Branches[DefaultBranch].Tip.Sha.Should().BeEquivalentTo(commit.Sha);
        }

        [Fact]
        public async Task BranchTargetNoLocal()
        {
            using var repoPath = GetRepository(out var remote, out var local);
            var jbName = "Jumpback";
            string tipSha;
            string commitSha;

            {
                using var repo = new Repository(local);
                tipSha = repo.Head.Tip.Sha;
                var jbBranch = repo.CreateBranch(jbName);
                repo.Branches.Update(
                    jbBranch,
                    b => b.Remote = repo.Network.Remotes.First().Name,
                    b => b.UpstreamBranch = jbBranch.CanonicalName);
                repo.Network.Push(jbBranch);
                repo.Branches.Remove(jbName);
                commitSha = AddACommit(local).Sha;
                repo.Head.Tip.Sha.Should().NotBeEquivalentTo(tipSha);
                repo.Network.Push(repo.Branches[DefaultBranch]);
            }

            var clonePath = Path.Combine(repoPath.Dir.Path, "Clone");
            using var clone = new Repository(Repository.Clone(remote, clonePath));

            var versioning = new GitPatcherVersioning(PatcherVersioningEnum.Branch, $"origin/{jbName}");
            var resp = await GitPatcherRun.CheckoutRunnerRepository(
                proj: ProjPath,
                localRepoDir: clonePath,
                patcherVersioning: versioning,
                nugetVersioning: TypicalNugetVersioning(),
                logger: null,
                cancel: CancellationToken.None,
                compile: false);
            resp.IsHaltingError.Should().BeFalse();
            resp.RunnableState.Succeeded.Should().BeTrue();
            clone.Head.Tip.Sha.Should().BeEquivalentTo(tipSha);
            clone.Head.FriendlyName.Should().BeEquivalentTo(GitPatcherRun.RunnerBranch);
            clone.Branches[jbName].Should().BeNull();
            clone.Branches[DefaultBranch].Tip.Sha.Should().BeEquivalentTo(commitSha);
        }

        [Fact]
        public async Task BranchNewContent()
        {
            using var repoPath = GetRepository(out var remote, out var local);
            string tipSha;
            string commitSha;

            var clonePath = Path.Combine(repoPath.Dir.Path, "Clone");
            using var clone = new Repository(Repository.Clone(remote, clonePath));
            {
                using var repo = new Repository(local);
                tipSha = repo.Head.Tip.Sha;
                commitSha = AddACommit(local).Sha;
                repo.Head.Tip.Sha.Should().NotBeEquivalentTo(tipSha);
                repo.Network.Push(repo.Branches[DefaultBranch]);
            }

            var versioning = new GitPatcherVersioning(PatcherVersioningEnum.Branch, $"origin/{DefaultBranch}");
            var resp = await GitPatcherRun.CheckoutRunnerRepository(
                proj: ProjPath,
                localRepoDir: clonePath,
                patcherVersioning: versioning,
                nugetVersioning: TypicalNugetVersioning(),
                logger: null,
                cancel: CancellationToken.None,
                compile: false);
            resp.IsHaltingError.Should().BeFalse();
            resp.RunnableState.Succeeded.Should().BeTrue();
            clone.Head.Tip.Sha.Should().BeEquivalentTo(commitSha);
            clone.Head.FriendlyName.Should().BeEquivalentTo(GitPatcherRun.RunnerBranch);
            clone.Branches[DefaultBranch].Tip.Sha.Should().BeEquivalentTo(tipSha);
        }
        #endregion
    }
}
