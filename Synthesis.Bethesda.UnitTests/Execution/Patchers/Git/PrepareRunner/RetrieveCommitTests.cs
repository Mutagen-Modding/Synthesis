using AutoFixture.Xunit2;
using FluentAssertions;
using NSubstitute;
using Noggog.GitRepository;
using Synthesis.Bethesda.Execution.Patchers.Git.Services;
using Synthesis.Bethesda.Execution.Patchers.Git.Services.PrepareRunner;
using Synthesis.Bethesda.UnitTests.AutoData;

namespace Synthesis.Bethesda.UnitTests.Execution.Patchers.Git.PrepareRunner;

public class RetrieveCommitTests
{
    [Theory, SynthAutoData]
    public void PassesShaToTryGetCommit(
        [Frozen]IGitRepository repo,
        RepoTarget targets,
        GitPatcherVersioning patcherVersioning,
        CancellationToken cancel,
        RetrieveCommit sut)
    {
        sut.TryGet(repo, targets, patcherVersioning, cancel);
        repo.Received(1).TryGetCommit(targets.TargetSha, out Arg.Any<bool>());
    }
        
    [Theory, SynthAutoData]
    public void FailIfNotValidSha(
        [Frozen]IGitRepository repo,
        ICommit commit,
        RepoTarget targets,
        GitPatcherVersioning patcherVersioning,
        CancellationToken cancel,
        RetrieveCommit sut)
    {
        repo.TryGetCommit(default!, out _).ReturnsForAnyArgs(x =>
        {
            x[1] = false;
            return commit;
        });
        sut.TryGet(repo, targets, patcherVersioning, cancel)
            .Succeeded.Should().BeFalse();
    }
        
    [Theory, SynthAutoData]
    public void SuccessIfCommitNotNull(
        [Frozen]IGitRepository repo,
        ICommit commit,
        RepoTarget targets,
        GitPatcherVersioning patcherVersioning,
        CancellationToken cancel,
        RetrieveCommit sut)
    {
        repo.TryGetCommit(default!, out _).ReturnsForAnyArgs(x =>
        {
            x[1] = true;
            return commit;
        });
        sut.TryGet(repo, targets, patcherVersioning, cancel)
            .Succeeded.Should().BeTrue();
    }
        
    [Theory, SynthAutoData]
    public void QueryForFetchIfCommitNull(
        [Frozen]IGitRepository repo,
        RepoTarget targets,
        GitPatcherVersioning patcherVersioning,
        CancellationToken cancel,
        RetrieveCommit sut)
    {
        repo.TryGetCommit(default!, out _).ReturnsForAnyArgs(x =>
        {
            x[1] = true;
            return default(ICommit?);
        });
        sut.TryGet(repo, targets, patcherVersioning, cancel);
        sut.ShouldFetchIfMissing.Received(1).Should(patcherVersioning);
    }
        
    [Theory, SynthAutoData]
    public void ShouldNotFetchReturnsFailure(
        [Frozen]IGitRepository repo,
        RepoTarget targets,
        GitPatcherVersioning patcherVersioning,
        CancellationToken cancel,
        RetrieveCommit sut)
    {
        repo.TryGetCommit(default!, out _).ReturnsForAnyArgs(x =>
        {
            x[1] = true;
            return default(ICommit?);
        });
        sut.ShouldFetchIfMissing.Should(default!).ReturnsForAnyArgs(false);
        sut.TryGet(repo, targets, patcherVersioning, cancel)
            .Succeeded.Should().BeFalse();
    }
        
    [Theory, SynthAutoData]
    public void ShouldFetchDoesFetch(
        [Frozen]IGitRepository repo,
        RepoTarget targets,
        GitPatcherVersioning patcherVersioning,
        CancellationToken cancel,
        RetrieveCommit sut)
    {
        repo.TryGetCommit(default!, out _).ReturnsForAnyArgs(x =>
        {
            x[1] = true;
            return default(ICommit?);
        });
        sut.ShouldFetchIfMissing.Should(default!).ReturnsForAnyArgs(true);
        sut.TryGet(repo, targets, patcherVersioning, cancel);
        repo.Received(1).Fetch();
    }
        
    [Theory, SynthAutoData]
    public void SecondTryGetCommitFailsReturnsFailure(
        [Frozen]IGitRepository repo,
        RepoTarget targets,
        GitPatcherVersioning patcherVersioning,
        CancellationToken cancel,
        RetrieveCommit sut)
    {
        repo.TryGetCommit(default!, out _).ReturnsForAnyArgs(
            x =>
            {
                x[1] = true;
                return default(ICommit?);
            });
        sut.ShouldFetchIfMissing.Should(default!).ReturnsForAnyArgs(true);
        sut.TryGet(repo, targets, patcherVersioning, cancel)
            .Succeeded.Should().BeFalse();
    }
        
    [Theory, SynthAutoData]
    public void SecondTryGetCommitSuccessReturnsSuccess(
        [Frozen]IGitRepository repo,
        RepoTarget targets,
        ICommit commit,
        GitPatcherVersioning patcherVersioning,
        CancellationToken cancel,
        RetrieveCommit sut)
    {
        repo.TryGetCommit(default!, out _).ReturnsForAnyArgs(
            x =>
            {
                x[1] = true;
                return default(ICommit?);
            },
            x =>
            {
                x[1] = true;
                return commit;
            });
        sut.ShouldFetchIfMissing.Should(default!).ReturnsForAnyArgs(true);
        sut.TryGet(repo, targets, patcherVersioning, cancel)
            .Succeeded.Should().BeTrue();
    }
}