using System.Diagnostics;
using FluentAssertions;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using Noggog;
using NSubstitute;
using Noggog.GitRepository;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Patchers.Git.ModifyProject;
using Synthesis.Bethesda.Execution.Patchers.Git.PrepareRunner;
using Synthesis.Bethesda.Execution.Patchers.Solution;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.Execution.Versioning;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;
using ILogger = Serilog.ILogger;

namespace Synthesis.Bethesda.UnitTests.Execution.Patchers.Git.PrepareRunner;

public class PrepareRunnerRepositoryIntegrationTests : RepoTestUtility
{
    private PrepareRunnerRepository Get(DirectoryPath local)
    {
        var availableProjectsRetriever = new AvailableProjectsRetriever(
            Substitute.For<ILogger>(),
            IFileSystemExt.DefaultFilesystem);
        var modify = Substitute.For<IModifyRunnerProjects>();
        modify.WhenForAnyArgs(x => x.Modify(default!, default!, default!, out _)).Do(x =>
        {
            x[3] = new NugetVersionPair(null, null);
        });
        var runnerRepoDirectoryInjection = new RunnerRepoDirectoryInjection(local);
        return new PrepareRunnerRepository(
            Substitute.For<ILogger>(),
            new SolutionFileLocator(
                IFileSystemExt.DefaultFilesystem),
            new RunnerRepoProjectPathRetriever(
                IFileSystemExt.DefaultFilesystem,
                runnerRepoDirectoryInjection,
                availableProjectsRetriever,
            Substitute.For<ILogger>()),
            modify,
            new ResetToTarget(
                Substitute.For<ILogger>(),
                new CheckoutRunnerBranch(),
                new GetRepoTarget(),
                new RetrieveCommit(
                    new ShouldFetchIfMissing())),
            new BuildMetaFilePathProviderInjection(Path.Combine(local, "Build.meta")),
            runnerRepoDirectoryInjection,
            new ProvideRepositoryCheckouts(
                Substitute.For<ILogger<ProvideRepositoryCheckouts>>(),
                new GitRepositoryFactory()));
    }
        
    public Commit AddACommit(string path)
    {
        File.AppendAllText(Path.Combine(path, AFile), "Hello there");
        using var repo = new Repository(path);
        LibGit2Sharp.Commands.Stage(repo, AFile);
        var sig = Signature;
        var commit = repo.Commit("A commit", sig, sig);
        repo.Network.Push(repo.Head);
        return commit;
    }

    [DebuggerStepThrough]
    public GitPatcherVersioning TypicalPatcherVersioning() =>
        new(
            PatcherVersioningEnum.Branch,
            DefaultBranch);

    [DebuggerStepThrough]
    public NugetsVersioningTarget TypicalNugetVersioning() =>
        new(
            new NugetVersioningTarget(null, NugetVersioningEnum.Match),
            new NugetVersioningTarget(null, NugetVersioningEnum.Match));

    #region Checkout Runner
    [Theory, SynthAutoData]
    public async Task ThrowsIfCancelled(
        CheckoutInput checkoutInput,
        CancellationToken cancelledToken)
    {
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
        {
            return Get(string.Empty).Checkout(
                checkoutInput,
                cancel: cancelledToken);
        });
    }

    [Fact]
    public async Task CreatesRunnerBranch()
    {
        using var repoPath = GetRepository(
            nameof(PrepareRunnerRepositoryIntegrationTests),
            out var remote, out var local);
        var resp = await Get(local).Checkout(
            new CheckoutInput(
                ProjPath,
                TypicalPatcherVersioning(),
                TypicalNugetVersioning()),
            cancel: CancellationToken.None);
        using var repo = new Repository(local);
        repo.Branches.Select(x => x.FriendlyName).Should().Contain(CheckoutRunnerBranch.RunnerBranch);
    }

    [Fact]
    public async Task NonExistentProjPath()
    {
        using var repoPath = GetRepository(
            nameof(PrepareRunnerRepositoryIntegrationTests),
            out var remote, out var local);
        var resp = await Get(local).Checkout(
            new CheckoutInput(
                string.Empty,
                TypicalPatcherVersioning(),
                TypicalNugetVersioning()),
            cancel: CancellationToken.None);
        resp.IsHaltingError.Should().BeTrue();
        resp.RunnableState.Succeeded.Should().BeFalse();
        resp.RunnableState.Reason.Should().Contain("Could not locate target project file");
    }

    [Fact]
    public async Task NonExistentSlnPath()
    {
        using var repoPath = GetRepository(
            nameof(PrepareRunnerRepositoryIntegrationTests),
            out var remote, out var local,
            createPatcherFiles: false);
        var resp = await Get(local).Checkout(
            new CheckoutInput(
                ProjPath,
                TypicalPatcherVersioning(),
                TypicalNugetVersioning()),
            cancel: CancellationToken.None);
        resp.IsHaltingError.Should().BeTrue();
        resp.RunnableState.Succeeded.Should().BeFalse();
        resp.RunnableState.Reason.Should().Contain("Could not locate solution");
    }

    [Fact]
    public async Task UnknownSha()
    {
        using var repoPath = GetRepository(
            nameof(PrepareRunnerRepositoryIntegrationTests),
            out var remote, out var local);
        var versioning = new GitPatcherVersioning(PatcherVersioningEnum.Commit, "46c207318c1531de7dc2f8e8c2a91aced183bc30");
        var resp = await Get(local).Checkout(
            new CheckoutInput(
                ProjPath,
                versioning,
                TypicalNugetVersioning()),
            cancel: CancellationToken.None);
        resp.IsHaltingError.Should().BeTrue();
        resp.RunnableState.Succeeded.Should().BeFalse();
        resp.RunnableState.Reason.Should().Contain("Could not locate commit with given sha");
    }

    [Fact]
    public async Task MalformedSha()
    {
        using var repoPath = GetRepository(
            nameof(PrepareRunnerRepositoryIntegrationTests),
            out var remote, out var local);
        var versioning = new GitPatcherVersioning(PatcherVersioningEnum.Commit, "derp");
        var resp = await Get(local).Checkout(
            new CheckoutInput(
                ProjPath,
                versioning,
                TypicalNugetVersioning()),
            cancel: CancellationToken.None);
        resp.IsHaltingError.Should().BeTrue();
        resp.RunnableState.Succeeded.Should().BeFalse();
        resp.RunnableState.Reason.Should().Contain("Malformed sha string");
    }

    [Fact]
    public async Task TagTarget()
    {
        using var repoPath = GetRepository(
            nameof(PrepareRunnerRepositoryIntegrationTests),
            out var remote, out var local);
        var tagStr = "1.3.4";
        using var repo = new Repository(local);
        var tipSha = repo.Head.Tip.Sha;
        repo.Tags.Add(tagStr, tipSha);
        var versioning = new GitPatcherVersioning(PatcherVersioningEnum.Tag, tagStr);
        var resp = await Get(local).Checkout(
            new CheckoutInput(
                ProjPath,
                versioning,
                TypicalNugetVersioning()),
            cancel: CancellationToken.None);
        resp.RunnableState.Exception.Should().BeNull();
        resp.IsHaltingError.Should().BeFalse();
        resp.RunnableState.Succeeded.Should().BeTrue();
        repo.Head.Tip.Sha.Should().BeEquivalentTo(tipSha);
        repo.Head.FriendlyName.Should().BeEquivalentTo(CheckoutRunnerBranch.RunnerBranch);
    }

    [Fact]
    public async Task TagTargetNoLocal()
    {
        using var repoPath = GetRepository(
            nameof(PrepareRunnerRepositoryIntegrationTests),
            out var remote, out var local);
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
        var resp = await Get(clonePath).Checkout(
            new CheckoutInput(
                ProjPath,
                versioning,
                TypicalNugetVersioning()),
            cancel: CancellationToken.None);
        resp.RunnableState.Exception.Should().BeNull();
        resp.IsHaltingError.Should().BeFalse();
        resp.RunnableState.Succeeded.Should().BeTrue();
        clone.Head.Tip.Sha.Should().BeEquivalentTo(tipSha);
        clone.Head.FriendlyName.Should().BeEquivalentTo(CheckoutRunnerBranch.RunnerBranch);
    }

    [Fact]
    public async Task TagTargetJumpback()
    {
        using var repoPath = GetRepository(
            nameof(PrepareRunnerRepositoryIntegrationTests),
            out var remote, out var local);
        var tag = "1.3.4";
        using var repo = new Repository(local);
        var tipSha = repo.Head.Tip.Sha;
        repo.Tags.Add("1.3.4", tipSha);

        AddACommit(local);
        repo.Head.Tip.Sha.Should().NotBeEquivalentTo(tipSha);

        var versioning = new GitPatcherVersioning(PatcherVersioningEnum.Tag, tag);
        var resp = await Get(local).Checkout(
            new CheckoutInput(
                ProjPath,
                versioning,
                TypicalNugetVersioning()),
            cancel: CancellationToken.None);
        resp.RunnableState.Exception.Should().BeNull();
        resp.IsHaltingError.Should().BeFalse();
        resp.RunnableState.Succeeded.Should().BeTrue();
        repo.Head.Tip.Sha.Should().BeEquivalentTo(tipSha);
        repo.Head.FriendlyName.Should().BeEquivalentTo(CheckoutRunnerBranch.RunnerBranch);
    }

    [Fact]
    public async Task TagTargetNewContent()
    {
        using var repoPath = GetRepository(
            nameof(PrepareRunnerRepositoryIntegrationTests),
            out var remote, out var local);

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
        var resp = await Get(clonePath).Checkout(
            new CheckoutInput(
                ProjPath,
                versioning,
                TypicalNugetVersioning()),
            cancel: CancellationToken.None);
        resp.RunnableState.Exception.Should().BeNull();
        resp.IsHaltingError.Should().BeFalse();
        resp.RunnableState.Succeeded.Should().BeTrue();
        clone.Head.Tip.Sha.Should().BeEquivalentTo(commitSha);
        clone.Head.FriendlyName.Should().BeEquivalentTo(CheckoutRunnerBranch.RunnerBranch);
    }

    [Fact]
    public async Task CommitTargetJumpback()
    {
        using var repoPath = GetRepository(
            nameof(PrepareRunnerRepositoryIntegrationTests),
            out var remote, out var local);
        using var repo = new Repository(local);
        var tipSha = repo.Head.Tip.Sha;

        AddACommit(local);
        repo.Head.Tip.Sha.Should().NotBeEquivalentTo(tipSha);

        var versioning = new GitPatcherVersioning(PatcherVersioningEnum.Commit, tipSha);
        var resp = await Get(local).Checkout(
            new CheckoutInput(
                ProjPath,
                versioning,
                TypicalNugetVersioning()),
            cancel: CancellationToken.None);
        resp.RunnableState.Exception.Should().BeNull();
        resp.IsHaltingError.Should().BeFalse();
        resp.RunnableState.Succeeded.Should().BeTrue();
        repo.Head.Tip.Sha.Should().BeEquivalentTo(tipSha);
        repo.Head.FriendlyName.Should().BeEquivalentTo(CheckoutRunnerBranch.RunnerBranch);
    }

    [Fact]
    public async Task CommitTargetNoLocal()
    {
        using var repoPath = GetRepository(
            nameof(PrepareRunnerRepositoryIntegrationTests),
            out var remote, out var local);
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
        var resp = await Get(clonePath).Checkout(
            new CheckoutInput(
                ProjPath,
                versioning,
                TypicalNugetVersioning()),
            cancel: CancellationToken.None);
        resp.RunnableState.Exception.Should().BeNull();
        resp.IsHaltingError.Should().BeFalse();
        resp.RunnableState.Succeeded.Should().BeTrue();
        clone.Head.Tip.Sha.Should().BeEquivalentTo(tipSha);
        clone.Head.FriendlyName.Should().BeEquivalentTo(CheckoutRunnerBranch.RunnerBranch);
    }

    [Fact]
    public async Task CommitTargetNewContent()
    {
        using var repoPath = GetRepository(
            nameof(PrepareRunnerRepositoryIntegrationTests),
            out var remote, out var local);

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
        var resp = await Get(clonePath).Checkout(
            new CheckoutInput(
                ProjPath,
                versioning,
                TypicalNugetVersioning()),
            cancel: CancellationToken.None);
        resp.RunnableState.Exception.Should().BeNull();
        resp.IsHaltingError.Should().BeFalse();
        resp.RunnableState.Succeeded.Should().BeTrue();
        clone.Head.Tip.Sha.Should().BeEquivalentTo(commitSha);
        clone.Head.FriendlyName.Should().BeEquivalentTo(CheckoutRunnerBranch.RunnerBranch);
    }

    [Fact]
    public async Task BranchTargetJumpback()
    {
        using var repoPath = GetRepository(
            nameof(PrepareRunnerRepositoryIntegrationTests),
            out var remote, out var local);
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
        var resp = await Get(local).Checkout(
            new CheckoutInput(
                ProjPath,
                versioning,
                TypicalNugetVersioning()),
            cancel: CancellationToken.None);
        resp.RunnableState.Exception.Should().BeNull();
        resp.IsHaltingError.Should().BeFalse();
        resp.RunnableState.Succeeded.Should().BeTrue();
        repo.Head.Tip.Sha.Should().BeEquivalentTo(tipSha);
        repo.Head.FriendlyName.Should().BeEquivalentTo(CheckoutRunnerBranch.RunnerBranch);
        repo.Branches[jbName].Tip.Sha.Should().BeEquivalentTo(tipSha);
        repo.Branches[DefaultBranch].Tip.Sha.Should().BeEquivalentTo(commit.Sha);
    }

    [Fact]
    public async Task BranchTargetNoLocal()
    {
        using var repoPath = GetRepository(
            nameof(PrepareRunnerRepositoryIntegrationTests),
            out var remote, out var local);
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
        var resp = await Get(clonePath).Checkout(
            new CheckoutInput(
                ProjPath,
                versioning,
                TypicalNugetVersioning()),
            cancel: CancellationToken.None);
        resp.RunnableState.Exception.Should().BeNull();
        resp.IsHaltingError.Should().BeFalse();
        resp.RunnableState.Succeeded.Should().BeTrue();
        clone.Head.Tip.Sha.Should().BeEquivalentTo(tipSha);
        clone.Head.FriendlyName.Should().BeEquivalentTo(CheckoutRunnerBranch.RunnerBranch);
        clone.Branches[jbName].Should().BeNull();
        clone.Branches[DefaultBranch].Tip.Sha.Should().BeEquivalentTo(commitSha);
    }

    [Fact]
    public async Task BranchNewContent()
    {
        using var repoPath = GetRepository(
            nameof(PrepareRunnerRepositoryIntegrationTests),
            out var remote, out var local);
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
        var resp = await Get(clonePath).Checkout(
            new CheckoutInput(
                ProjPath,
                versioning,
                TypicalNugetVersioning()),
            cancel: CancellationToken.None);
        resp.RunnableState.Exception.Should().BeNull();
        resp.IsHaltingError.Should().BeFalse();
        resp.RunnableState.Succeeded.Should().BeTrue();
        clone.Head.Tip.Sha.Should().BeEquivalentTo(commitSha);
        clone.Head.FriendlyName.Should().BeEquivalentTo(CheckoutRunnerBranch.RunnerBranch);
        clone.Branches[DefaultBranch].Tip.Sha.Should().BeEquivalentTo(tipSha);
    }
    #endregion
}