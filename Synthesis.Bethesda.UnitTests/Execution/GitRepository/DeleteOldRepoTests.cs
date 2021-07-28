using System;
using System.IO;
using System.Linq;
using Serilog;
using Xunit;
using AutoFixture;
using FluentAssertions;
using LibGit2Sharp;
using Noggog;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Synthesis.Bethesda.Execution.GitRepository;
using Synthesis.Bethesda.UnitTests.AutoData;

namespace Synthesis.Bethesda.UnitTests.Execution.GitRepository
{
    public class DeleteOldRepoTests : RepoTestUtility
    {
        [Theory, SynthAutoData]
        public void IsRepositoryDesirable(DeleteOldRepo sut)
        {
            using var repoPath = GetRepository(
                nameof(DeleteOldRepoTests),
                out var remote, out var local,
                createPatcherFiles: false);
            using var repo = new Bethesda.Execution.GitRepository.GitRepository(new Repository(local));
            sut.IsRepositoryUndesirable(repo)
                .Should().BeFalse();
        }

        [Theory, SynthAutoData(UseMockRepositoryProvider: true)]
        public void Keep(DeleteOldRepo sut)
        {
            using var repoPath = GetRepository(
                nameof(DeleteOldRepoTests),
                out var remote, out var local,
                createPatcherFiles: false);
            using var tmp = Utility.GetTempFolder(nameof(DeleteOldRepoTests));
            sut.CheckIfKeeping(local, remote.Path)
                .Should().BeTrue();
            local.Exists.Should().BeTrue();
        }

        [Theory, SynthAutoData(UseMockRepositoryProvider: false)]
        public void Exception(DeleteOldRepo sut)
        {
            using var repoPath = GetRepository(
                nameof(DeleteOldRepoTests),
                out var remote, out var local,
                createPatcherFiles: false);
            sut.RepoCheckouts.Get(default).ThrowsForAnyArgs(new RepositoryNotFoundException());
            sut.CheckIfKeeping(local, remote.Path)
                .Should().BeFalse();
            local.Exists.Should().BeFalse();
        }

        [Theory, SynthAutoData]
        public void DoesNotExist(DeleteOldRepo sut)
        {
            var tmp = Utility.GetTempFolder(nameof(DeleteOldRepoTests));
            sut.CheckIfKeeping(Path.Combine(tmp.Dir.Path, "Nothing"), GetResponse<string>.Failure)
                .Should().BeFalse();
        }

        [Theory, SynthAutoData]
        public void RemoteFailed(DeleteOldRepo sut)
        {
            using var repoPath = GetRepository(
                nameof(DeleteOldRepoTests),
                out var remote, out var local,
                createPatcherFiles: false);
            sut.CheckIfKeeping(local, GetResponse<string>.Failure)
                .Should().BeFalse();
            local.Exists.Should().BeFalse();
        }

        [Theory, SynthAutoData]
        public void RemoteDifferent(
            RepositoryCheckout checkout,
            DeleteOldRepo sut)
        {
            using var repoPath = GetRepository(
                nameof(DeleteOldRepoTests),
                out var remote, out var local,
                createPatcherFiles: false);
            sut.RepoCheckouts.Get(default).ThrowsForAnyArgs(new RepositoryNotFoundException());
            sut.CheckIfKeeping(local, GetResponse<string>.Succeed("SomethingElse"))
                .Should().BeFalse();
            local.Exists.Should().BeFalse();
        }
    }
}