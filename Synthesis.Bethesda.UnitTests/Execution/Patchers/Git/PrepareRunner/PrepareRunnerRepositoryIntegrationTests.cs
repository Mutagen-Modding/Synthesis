using System.Diagnostics;
using FluentAssertions;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using Noggog;
using NSubstitute;
using Noggog.GitRepository;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Patchers.Git.Services;
using Synthesis.Bethesda.Execution.Patchers.Git.Services.ModifyProject;
using Synthesis.Bethesda.Execution.Patchers.Git.Services.PrepareRunner;
using Synthesis.Bethesda.Execution.Patchers.Solution;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.Execution.Versioning;
using Synthesis.Bethesda.UnitTests.AutoData;
using ILogger = Serilog.ILogger;

namespace Synthesis.Bethesda.UnitTests.Execution.Patchers.Git.PrepareRunner;

public class PrepareRunnerRepositoryIntegrationTests
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
        
    public Commit AddACommit(RepoTestUtilityPayload payload, string path)
    {
        File.AppendAllText(Path.Combine(path, payload.AFile), "Hello there");
        using var repo = new Repository(path);
        LibGit2Sharp.Commands.Stage(repo, payload.AFile);
        var sig = payload.Signature;
        var commit = repo.Commit("A commit", sig, sig);
        repo.Network.Push(repo.Head);
        return commit;
    }

    [DebuggerStepThrough]
    public GitPatcherVersioning TypicalPatcherVersioning(string defaultBranchName) =>
        new(
            PatcherVersioningEnum.Branch,
            defaultBranchName);

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
        using var util = RepoTestUtilityPayload.GetRepository(
            nameof(PrepareRunnerRepositoryIntegrationTests));
        var resp = await Get(util.Local).Checkout(
            new CheckoutInput(
                util.ProjPath,
                TypicalPatcherVersioning(util.DefaultBranchName),
                TypicalNugetVersioning()),
            cancel: CancellationToken.None);
        using var repo = new Repository(util.Local);
        repo.Branches.Select(x => x.FriendlyName).Should().Contain(CheckoutRunnerBranch.RunnerBranch);
    }

    [Fact]
    public async Task NonExistentProjPath()
    {
        using var util = RepoTestUtilityPayload.GetRepository(
            nameof(PrepareRunnerRepositoryIntegrationTests));
        var resp = await Get(util.Local).Checkout(
            new CheckoutInput(
                string.Empty,
                TypicalPatcherVersioning(util.DefaultBranchName),
                TypicalNugetVersioning()),
            cancel: CancellationToken.None);
        resp.IsHaltingError.Should().BeTrue();
        resp.RunnableState.Succeeded.Should().BeFalse();
        resp.RunnableState.Reason.Should().Contain("Could not locate target project file");
    }

    [Fact]
    public async Task NonExistentSlnPath()
    {
        using var util = RepoTestUtilityPayload.GetRepository(
            nameof(PrepareRunnerRepositoryIntegrationTests),
            createPatcherFiles: false);
        var resp = await Get(util.Local).Checkout(
            new CheckoutInput(
                util.ProjPath,
                TypicalPatcherVersioning(util.DefaultBranchName),
                TypicalNugetVersioning()),
            cancel: CancellationToken.None);
        resp.IsHaltingError.Should().BeTrue();
        resp.RunnableState.Succeeded.Should().BeFalse();
        resp.RunnableState.Reason.Should().Contain("Could not locate solution");
    }

    [Fact]
    public async Task UnknownSha()
    {
        using var util = RepoTestUtilityPayload.GetRepository(
            nameof(PrepareRunnerRepositoryIntegrationTests));
        var versioning = new GitPatcherVersioning(PatcherVersioningEnum.Commit, "46c207318c1531de7dc2f8e8c2a91aced183bc30");
        var resp = await Get(util.Local).Checkout(
            new CheckoutInput(
                util.ProjPath,
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
        using var util = RepoTestUtilityPayload.GetRepository(
            nameof(PrepareRunnerRepositoryIntegrationTests));
        var versioning = new GitPatcherVersioning(PatcherVersioningEnum.Commit, "derp");
        var resp = await Get(util.Local).Checkout(
            new CheckoutInput(
                util.ProjPath,
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
        using var util = RepoTestUtilityPayload.GetRepository(
            nameof(PrepareRunnerRepositoryIntegrationTests));
        var tagStr = "1.3.4";
        using var repo = new Repository(util.Local);
        var tipSha = repo.Head.Tip.Sha;
        repo.Tags.Add(tagStr, tipSha);
        var versioning = new GitPatcherVersioning(PatcherVersioningEnum.Tag, tagStr);
        var resp = await Get(util.Local).Checkout(
            new CheckoutInput(
                util.ProjPath,
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
        using var util = RepoTestUtilityPayload.GetRepository(
            nameof(PrepareRunnerRepositoryIntegrationTests));
        var tagStr = "1.3.4";
        string tipSha;
        {
            using var repo = new Repository(util.Local);
            tipSha = repo.Head.Tip.Sha;
            var tag = repo.Tags.Add(tagStr, tipSha);
            repo.Network.Push(repo.Network.Remotes.First(), tag.CanonicalName);

            AddACommit(util, util.Local);
            repo.Head.Tip.Sha.Should().NotBeEquivalentTo(tipSha);
        }

        var clonePath = Path.Combine(util.Temp.Path, "Clone");
        using var clone = new Repository(Repository.Clone(util.Remote, clonePath));

        var versioning = new GitPatcherVersioning(PatcherVersioningEnum.Tag, tagStr);
        var resp = await Get(clonePath).Checkout(
            new CheckoutInput(
                util.ProjPath,
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
        using var util = RepoTestUtilityPayload.GetRepository(
            nameof(PrepareRunnerRepositoryIntegrationTests));
        var tag = "1.3.4";
        using var repo = new Repository(util.Local);
        var tipSha = repo.Head.Tip.Sha;
        repo.Tags.Add("1.3.4", tipSha);

        AddACommit(util, util.Local);
        repo.Head.Tip.Sha.Should().NotBeEquivalentTo(tipSha);

        var versioning = new GitPatcherVersioning(PatcherVersioningEnum.Tag, tag);
        var resp = await Get(util.Local).Checkout(
            new CheckoutInput(
                util.ProjPath,
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
        using var util = RepoTestUtilityPayload.GetRepository(
            nameof(PrepareRunnerRepositoryIntegrationTests));

        var clonePath = Path.Combine(util.Temp.Path, "Clone");
        using var clone = new Repository(Repository.Clone(util.Remote, clonePath));

        var tagStr = "1.3.4";
        string commitSha;
        {
            using var repo = new Repository(util.Local);
            util.AddACommit();
            commitSha = repo.Head.Tip.Sha;
            var tag = repo.Tags.Add(tagStr, commitSha);
            repo.Network.Push(repo.Network.Remotes.First(), tag.CanonicalName);
        }

        var versioning = new GitPatcherVersioning(PatcherVersioningEnum.Tag, tagStr);
        var resp = await Get(clonePath).Checkout(
            new CheckoutInput(
                util.ProjPath,
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
        using var util = RepoTestUtilityPayload.GetRepository(
            nameof(PrepareRunnerRepositoryIntegrationTests));
        using var repo = new Repository(util.Local);
        var tipSha = repo.Head.Tip.Sha;

        util.AddACommit();
        repo.Head.Tip.Sha.Should().NotBeEquivalentTo(tipSha);

        var versioning = new GitPatcherVersioning(PatcherVersioningEnum.Commit, tipSha);
        var resp = await Get(util.Local).Checkout(
            new CheckoutInput(
                util.ProjPath,
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
        using var util = RepoTestUtilityPayload.GetRepository(
            nameof(PrepareRunnerRepositoryIntegrationTests));
        string tipSha;
        {
            using var repo = new Repository(util.Local);
            tipSha = repo.Head.Tip.Sha;

            util.AddACommit();
            repo.Head.Tip.Sha.Should().NotBeEquivalentTo(tipSha);
        }

        var clonePath = Path.Combine(util.Temp.Path, "Clone");
        using var clone = new Repository(Repository.Clone(util.Remote, clonePath));

        var versioning = new GitPatcherVersioning(PatcherVersioningEnum.Commit, tipSha);
        var resp = await Get(clonePath).Checkout(
            new CheckoutInput(
                util.ProjPath,
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
        using var util = RepoTestUtilityPayload.GetRepository(
            nameof(PrepareRunnerRepositoryIntegrationTests));

        var clonePath = Path.Combine(util.Temp.Path, "Clone");
        using var clone = new Repository(Repository.Clone(util.Remote, clonePath));

        string tipSha, commitSha;
        {
            using var repo = new Repository(util.Local);
            tipSha = repo.Head.Tip.Sha;

            commitSha = util.AddACommit().Sha;
            repo.Head.Tip.Sha.Should().NotBeEquivalentTo(tipSha);
        }

        var versioning = new GitPatcherVersioning(PatcherVersioningEnum.Commit, commitSha);
        var resp = await Get(clonePath).Checkout(
            new CheckoutInput(
                util.ProjPath,
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
        using var util = RepoTestUtilityPayload.GetRepository(
            nameof(PrepareRunnerRepositoryIntegrationTests));
        using var repo = new Repository(util.Local);
        var tipSha = repo.Head.Tip.Sha;
        var jbName = "Jumpback";
        var jbBranch = repo.CreateBranch(jbName);
        repo.Branches.Update(
            jbBranch,
            b => b.Remote = repo.Network.Remotes.First().Name,
            b => b.UpstreamBranch = jbBranch.CanonicalName);
        repo.Network.Push(jbBranch);

        var commit = util.AddACommit();
        repo.Head.Tip.Sha.Should().NotBeEquivalentTo(tipSha);

        var versioning = new GitPatcherVersioning(PatcherVersioningEnum.Branch, jbName);
        var resp = await Get(util.Local).Checkout(
            new CheckoutInput(
                util.ProjPath,
                versioning,
                TypicalNugetVersioning()),
            cancel: CancellationToken.None);
        resp.RunnableState.Exception.Should().BeNull();
        resp.IsHaltingError.Should().BeFalse();
        resp.RunnableState.Succeeded.Should().BeTrue();
        repo.Head.Tip.Sha.Should().BeEquivalentTo(tipSha);
        repo.Head.FriendlyName.Should().BeEquivalentTo(CheckoutRunnerBranch.RunnerBranch);
        repo.Branches[jbName].Tip.Sha.Should().BeEquivalentTo(tipSha);
        repo.Branches[util.DefaultBranchName].Tip.Sha.Should().BeEquivalentTo(commit.Sha);
    }

    [Fact]
    public async Task BranchTargetNoLocal()
    {
        using var util = RepoTestUtilityPayload.GetRepository(
            nameof(PrepareRunnerRepositoryIntegrationTests));
        var jbName = "Jumpback";
        string tipSha;
        string commitSha;

        {
            using var repo = new Repository(util.Local);
            tipSha = repo.Head.Tip.Sha;
            var jbBranch = repo.CreateBranch(jbName);
            repo.Branches.Update(
                jbBranch,
                b => b.Remote = repo.Network.Remotes.First().Name,
                b => b.UpstreamBranch = jbBranch.CanonicalName);
            repo.Network.Push(jbBranch);
            repo.Branches.Remove(jbName);
            commitSha = util.AddACommit().Sha;
            repo.Head.Tip.Sha.Should().NotBeEquivalentTo(tipSha);
            repo.Network.Push(repo.Branches[util.DefaultBranchName]);
        }

        var clonePath = Path.Combine(util.Temp.Path, "Clone");
        using var clone = new Repository(Repository.Clone(util.Remote, clonePath));

        var versioning = new GitPatcherVersioning(PatcherVersioningEnum.Branch, $"origin/{jbName}");
        var resp = await Get(clonePath).Checkout(
            new CheckoutInput(
                util.ProjPath,
                versioning,
                TypicalNugetVersioning()),
            cancel: CancellationToken.None);
        resp.RunnableState.Exception.Should().BeNull();
        resp.IsHaltingError.Should().BeFalse();
        resp.RunnableState.Succeeded.Should().BeTrue();
        clone.Head.Tip.Sha.Should().BeEquivalentTo(tipSha);
        clone.Head.FriendlyName.Should().BeEquivalentTo(CheckoutRunnerBranch.RunnerBranch);
        clone.Branches[jbName].Should().BeNull();
        clone.Branches[util.DefaultBranchName].Tip.Sha.Should().BeEquivalentTo(commitSha);
    }

    [Fact]
    public async Task BranchNewContent()
    {
        using var util = RepoTestUtilityPayload.GetRepository(
            nameof(PrepareRunnerRepositoryIntegrationTests));
        string tipSha;
        string commitSha;

        var clonePath = Path.Combine(util.Temp.Path, "Clone");
        using var clone = new Repository(Repository.Clone(util.Remote, clonePath));
        {
            using var repo = new Repository(util.Local);
            tipSha = repo.Head.Tip.Sha;
            commitSha = util.AddACommit().Sha;
            repo.Head.Tip.Sha.Should().NotBeEquivalentTo(tipSha);
            repo.Network.Push(repo.Branches[util.DefaultBranchName]);
        }

        var versioning = new GitPatcherVersioning(PatcherVersioningEnum.Branch, $"origin/{util.DefaultBranchName}");
        var resp = await Get(clonePath).Checkout(
            new CheckoutInput(
                util.ProjPath,
                versioning,
                TypicalNugetVersioning()),
            cancel: CancellationToken.None);
        resp.RunnableState.Exception.Should().BeNull();
        resp.IsHaltingError.Should().BeFalse();
        resp.RunnableState.Succeeded.Should().BeTrue();
        clone.Head.Tip.Sha.Should().BeEquivalentTo(commitSha);
        clone.Head.FriendlyName.Should().BeEquivalentTo(CheckoutRunnerBranch.RunnerBranch);
        clone.Branches[util.DefaultBranchName].Tip.Sha.Should().BeEquivalentTo(tipSha);
    }
    #endregion
}