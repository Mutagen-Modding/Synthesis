using System;
using System.IO;
using System.Linq;
using Serilog;
using Synthesis.Bethesda.Execution.GitRespository;
using Xunit;
using AutoFixture;
using FluentAssertions;
using LibGit2Sharp;
using Noggog;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Synthesis.Bethesda.UnitTests.Execution.GitRepository
{
    public class DeleteOldRepoTests : RepoTestUtility, IClassFixture<Fixture>
    {
        private readonly Fixture _Fixture;

        public DeleteOldRepoTests(Fixture fixture)
        {
            _Fixture = fixture;
        }

        private DeleteOldRepo Get()
        {
            return new DeleteOldRepo(
                _Fixture.Inject.Create<ILogger>(),
                new ProvideRepositoryCheckouts(_Fixture.Inject.Create<ILogger>()));
        }
        
        [Fact]
        public void IsRepositoryDesirable()
        {
            using var repoPath = GetRepository(
                nameof(DeleteOldRepoTests),
                out var remote, out var local,
                createPatcherFiles: false);
            var del = Get();
            using var repo = new Bethesda.Execution.GitRespository.GitRepository(new Repository(local));
            del.IsRepositoryUndesirable(repo)
                .Should().BeFalse();
        }

        [Fact]
        public void Keep()
        {
            using var repoPath = GetRepository(
                nameof(DeleteOldRepoTests),
                out var remote, out var local,
                createPatcherFiles: false);
            var tmp = Utility.GetTempFolder(nameof(DeleteOldRepoTests));
            var del = Get();
            del.CheckIfKeeping(local, remote.Path)
                .Should().BeTrue();
            local.Exists.Should().BeTrue();
        }

        [Fact]
        public void Exception()
        {
            using var repoPath = GetRepository(
                nameof(DeleteOldRepoTests),
                out var remote, out var local,
                createPatcherFiles: false);
            var provide = Substitute.For<IProvideRepositoryCheckouts>();
            provide.Get(_Fixture.Inject.Create<DirectoryPath>()).ThrowsForAnyArgs(new RepositoryNotFoundException());
            var del = new DeleteOldRepo(
                _Fixture.Inject.Create<ILogger>(),
                provide);
            del.CheckIfKeeping(local, remote.Path)
                .Should().BeFalse();
            local.Exists.Should().BeFalse();
        }

        [Fact]
        public void DoesNotExist()
        {
            var tmp = Utility.GetTempFolder(nameof(DeleteOldRepoTests));
            var del = Get();
            del.CheckIfKeeping(Path.Combine(tmp.Dir.Path, "Nothing"), GetResponse<string>.Failure)
                .Should().BeFalse();
        }

        [Fact]
        public void RemoteFailed()
        {
            using var repoPath = GetRepository(
                nameof(DeleteOldRepoTests),
                out var remote, out var local,
                createPatcherFiles: false);
            var del = Get();
            del.CheckIfKeeping(local, GetResponse<string>.Failure)
                .Should().BeFalse();
            local.Exists.Should().BeFalse();
        }

        [Fact]
        public void RemoteDifferent()
        {
            using var repoPath = GetRepository(
                nameof(DeleteOldRepoTests),
                out var remote, out var local,
                createPatcherFiles: false);
            var del = Get();
            del.CheckIfKeeping(local, GetResponse<string>.Succeed("SomethingElse"))
                .Should().BeFalse();
            local.Exists.Should().BeFalse();
        }
    }
}