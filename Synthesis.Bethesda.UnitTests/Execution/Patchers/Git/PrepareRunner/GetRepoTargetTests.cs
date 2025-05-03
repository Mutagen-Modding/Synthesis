using AutoFixture.Xunit2;
using Shouldly;
using NSubstitute;
using Noggog.GitRepository;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Patchers.Git.Services.PrepareRunner;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.UnitTests.AutoData;

namespace Synthesis.Bethesda.UnitTests.Execution.Patchers.Git.PrepareRunner;

public class GetRepoTargetTests
{
    #region Tag

    [Theory, SynthAutoData]
    public void TagTargetEmptyReturnsFail(
        [Frozen]IGitRepository repo,
        GetRepoTarget sut)
    {
        sut.Get(repo, new GitPatcherVersioning(PatcherVersioningEnum.Tag, string.Empty))
            .Succeeded.ShouldBeFalse();
    }
        
    [Theory, SynthAutoData]
    public void TagTargetFetches(
        [Frozen]IGitRepository repo,
        string target,
        GetRepoTarget sut)
    {
        sut.Get(repo, new GitPatcherVersioning(PatcherVersioningEnum.Tag, target));
        repo.Received(1).Fetch();
    }
        
    [Theory, SynthAutoData]
    public void TagPassesToRepo(
        [Frozen]IGitRepository repo,
        string target,
        GetRepoTarget sut)
    {
        sut.Get(repo, new GitPatcherVersioning(PatcherVersioningEnum.Tag, target));
        repo.Received(1).TryGetTagSha(target, out Arg.Any<string?>());
    }

    [Theory, SynthAutoData]
    public void TryGetTagFailsReturnsFail(
        [Frozen]IGitRepository repo,
        string target,
        GetRepoTarget sut)
    {
        repo.TryGetTagSha(default!, out _).ReturnsForAnyArgs(false);
        sut.Get(repo, new GitPatcherVersioning(PatcherVersioningEnum.Tag, target))
            .Succeeded.ShouldBeFalse();
    }
        
    [Theory, SynthAutoData]
    public void TagReturnsRepoTryGetTagSha(
        [Frozen]IGitRepository repo,
        string target,
        string sha,
        GetRepoTarget sut)
    {
        repo.TryGetTagSha(default!, out _).ReturnsForAnyArgs(x =>
        {
            x[1] = sha;
            return true;
        });
        var result = sut.Get(repo, new GitPatcherVersioning(PatcherVersioningEnum.Tag, target));
        result.Succeeded.ShouldBeTrue();
        result.Value.Target.ShouldBe(target);
        result.Value.TargetSha.ShouldBe(sha);
    }

    #endregion

    #region Commit

    [Theory, SynthAutoData]
    public void CommitTargetEmptyReturnsFail(
        [Frozen]IGitRepository repo,
        GetRepoTarget sut)
    {
        sut.Get(repo, new GitPatcherVersioning(PatcherVersioningEnum.Commit, string.Empty))
            .Succeeded.ShouldBeFalse();
    }
        
    [Theory, SynthAutoData]
    public void CommitTargetPassesAlong(
        [Frozen]IGitRepository repo,
        string target,
        GetRepoTarget sut)
    {
        var result = sut.Get(repo, new GitPatcherVersioning(PatcherVersioningEnum.Commit, target));
        result.Succeeded.ShouldBeTrue();
        result.Value.Target.ShouldBe(target);
        result.Value.TargetSha.ShouldBe(target);
    }

    #endregion

    #region Branch

    [Theory, SynthAutoData]
    public void BranchTargetEmptyReturnsFail(
        [Frozen]IGitRepository repo,
        GetRepoTarget sut)
    {
        sut.Get(repo, new GitPatcherVersioning(PatcherVersioningEnum.Branch, string.Empty))
            .Succeeded.ShouldBeFalse();
    }
        
    [Theory, SynthAutoData]
    public void BranchTargetFetches(
        [Frozen]IGitRepository repo,
        string target,
        GetRepoTarget sut)
    {
        sut.Get(repo, new GitPatcherVersioning(PatcherVersioningEnum.Branch, target));
        repo.Received(1).Fetch();
    }
        
    [Theory, SynthAutoData]
    public void BranchPassesToRepo(
        [Frozen]IGitRepository repo,
        string target,
        GetRepoTarget sut)
    {
        sut.Get(repo, new GitPatcherVersioning(PatcherVersioningEnum.Branch, target));
        repo.Received(1).TryGetBranch(target, out Arg.Any<IBranch?>());
    }
        
    [Theory, SynthAutoData]
    public void TryGetBranchFailsReturnsFail(
        [Frozen]IGitRepository repo,
        string target,
        GetRepoTarget sut)
    {
        repo.TryGetBranch(default!, out _).ReturnsForAnyArgs(false);
        sut.Get(repo, new GitPatcherVersioning(PatcherVersioningEnum.Branch, target))
            .Succeeded.ShouldBeFalse();
    }
        
    [Theory, SynthAutoData]
    public void BranchReturnsRepoTryGetBranchSha(
        [Frozen]IGitRepository repo,
        string target,
        IBranch branch,
        GetRepoTarget sut)
    {
        repo.TryGetBranch(default!, out _).ReturnsForAnyArgs(x =>
        {
            x[1] = branch;
            return true;
        });
        var result = sut.Get(repo, new GitPatcherVersioning(PatcherVersioningEnum.Branch, target));
        result.Succeeded.ShouldBeTrue();
        result.Value.Target.ShouldBe(target);
        result.Value.TargetSha.ShouldBe(branch.Tip.Sha);
    }
        
    [Theory, SynthAutoData]
    public void UnknownVersioningThrows(
        [Frozen]IGitRepository repo,
        string target,
        GetRepoTarget sut)
    {
        Assert.Throws<NotImplementedException>(() =>
        {
            sut.Get(repo, new GitPatcherVersioning((PatcherVersioningEnum)int.MaxValue, target));
        });
    }

    #endregion
}