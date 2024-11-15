using FluentAssertions;
using Noggog;
using NSubstitute;
using Noggog.GitRepository;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Patchers.Git.Services.PrepareRunner;
using Synthesis.Bethesda.UnitTests.AutoData;

namespace Synthesis.Bethesda.UnitTests.Execution.Patchers.Git.PrepareRunner;

public class ResetToTargetTests
{
    [Theory, SynthAutoData]
    public async Task CheckoutPassedToRunnerBranchCheckout(
        IGitRepository repo,
        GitPatcherVersioning patcherVersioning,
        CancellationToken cancel,
        ResetToTarget sut)
    {
        sut.Reset(repo, patcherVersioning, cancel);
        sut.CheckoutRunnerBranch.Received(1).Checkout(repo);
    }
        
    [Theory, SynthAutoData]
    public async Task GetRepoTargetCalled(
        IGitRepository repo,
        GitPatcherVersioning patcherVersioning,
        CancellationToken cancel,
        ResetToTarget sut)
    {
        sut.Reset(repo, patcherVersioning, cancel);
        sut.GetRepoTarget.Received(1).Get(repo, patcherVersioning);
    }
        
    [Theory, SynthAutoData]
    public async Task FailedGetRepoTargetReturnsFailure(
        IGitRepository repo,
        GitPatcherVersioning patcherVersioning,
        CancellationToken cancel,
        ResetToTarget sut)
    {
        sut.GetRepoTarget.Get(default!, default!).ReturnsForAnyArgs(GetResponse<RepoTarget>.Failure);
        var resp = sut.Reset(repo, patcherVersioning, cancel);
        resp.Succeeded.Should().BeFalse();
    }
        
    [Theory, SynthAutoData]
    public async Task RepoTargetPassedToRetrieveCommit(
        IGitRepository repo,
        RepoTarget target,
        GitPatcherVersioning patcherVersioning,
        CancellationToken cancel,
        ResetToTarget sut)
    {
        sut.GetRepoTarget.Get(default!, default!).ReturnsForAnyArgs(target);
        sut.Reset(repo, patcherVersioning, cancel);
        sut.RetrieveCommit.Received(1).TryGet(
            repo,
            target,
            patcherVersioning,
            cancel);
    }
        
    [Theory, SynthAutoData]
    public async Task FailedRetrieveCommitReturnsFailure(
        IGitRepository repo,
        GitPatcherVersioning patcherVersioning,
        CancellationToken cancel,
        ResetToTarget sut)
    {
        sut.RetrieveCommit.TryGet(default!, default!, default!, default)
            .ReturnsForAnyArgs(GetResponse<ICommit>.Failure);
        var resp = sut.Reset(repo, patcherVersioning, cancel);
        resp.Succeeded.Should().BeFalse();
    }
        
    [Theory, SynthAutoData]
    public async Task RetrievedCommitPassedToHardReset(
        IGitRepository repo,
        ICommit commit,
        RepoTarget target,
        GitPatcherVersioning patcherVersioning,
        CancellationToken cancel,
        ResetToTarget sut)
    {
        sut.GetRepoTarget.Get(default!, default!).ReturnsForAnyArgs(target);
        sut.RetrieveCommit.TryGet(default!, default!, default!, default)
            .ReturnsForAnyArgs(GetResponse<ICommit>.Succeed(commit));
        sut.Reset(repo, patcherVersioning, cancel);
        repo.Received(1).ResetHard(commit);
    }
}