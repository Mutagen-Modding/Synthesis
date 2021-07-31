using System;
using AutoFixture.Xunit2;
using FluentAssertions;
using NSubstitute;
using Synthesis.Bethesda.Execution.GitRepository;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Patchers.Git.PrepareRunner;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.Patchers.Git.PrepareRunner
{
    public class GetRepoTargetTests
    {
        #region Tag

        [Theory, SynthAutoData]
        public void TagTargetEmptyReturnsFail(
            [Frozen]IGitRepository repo,
            GetRepoTarget sut)
        {
            sut.Get(repo, new GitPatcherVersioning(PatcherVersioningEnum.Tag, string.Empty))
                .Succeeded.Should().BeFalse();
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
                .Succeeded.Should().BeFalse();
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
            result.Succeeded.Should().BeTrue();
            result.Value.Target.Should().Be(target);
            result.Value.TargetSha.Should().Be(sha);
        }

        #endregion

        #region Commit

        [Theory, SynthAutoData]
        public void CommitTargetEmptyReturnsFail(
            [Frozen]IGitRepository repo,
            GetRepoTarget sut)
        {
            sut.Get(repo, new GitPatcherVersioning(PatcherVersioningEnum.Commit, string.Empty))
                .Succeeded.Should().BeFalse();
        }
        
        [Theory, SynthAutoData]
        public void CommitTargetPassesAlong(
            [Frozen]IGitRepository repo,
            string target,
            GetRepoTarget sut)
        {
            var result = sut.Get(repo, new GitPatcherVersioning(PatcherVersioningEnum.Commit, target));
            result.Succeeded.Should().BeTrue();
            result.Value.Target.Should().Be(target);
            result.Value.TargetSha.Should().Be(target);
        }

        #endregion

        #region Branch

        [Theory, SynthAutoData]
        public void BranchTargetEmptyReturnsFail(
            [Frozen]IGitRepository repo,
            GetRepoTarget sut)
        {
            sut.Get(repo, new GitPatcherVersioning(PatcherVersioningEnum.Branch, string.Empty))
                .Succeeded.Should().BeFalse();
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
                .Succeeded.Should().BeFalse();
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
            result.Succeeded.Should().BeTrue();
            result.Value.Target.Should().Be(target);
            result.Value.TargetSha.Should().Be(branch.Tip.Sha);
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
}