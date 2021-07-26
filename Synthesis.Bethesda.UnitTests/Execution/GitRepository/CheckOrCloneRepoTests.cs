using System;
using System.Threading;
using FluentAssertions;
using Noggog;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Synthesis.Bethesda.Execution.GitRepository;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.GitRepository
{
    public class CheckOrCloneRepoTests
    {
        [Theory, SynthAutoData]
        public void CallsCheckIfKeeping(
            GetResponse<string> remote,
            DirectoryPath local,
            CheckOrCloneRepo sut)
        {
            sut.Delete.CheckIfKeeping(default, default).ReturnsForAnyArgs(true);
            var ret = sut.Check(
                remote,
                local,
                CancellationToken.None);
            sut.Delete.Received().CheckIfKeeping(local, remote);
            ret.Succeeded.Should().BeTrue();
            ret.Value.Remote.Should().Be(remote.Value);
            ret.Value.Local.Should().Be(local);
            sut.CloneRepo.DidNotReceiveWithAnyArgs().Clone(default!, default);
        }

        [Theory, SynthAutoData]
        public void RemoteFailed(
            DirectoryPath local,
            CheckOrCloneRepo sut)
        {
            sut.Delete.CheckIfKeeping(default, default).ReturnsForAnyArgs(false);
            var remote = GetResponse<string>.Fail("Fail string");
            var ret = sut.Check(
                remote,
                local,
                CancellationToken.None);
            ret.Succeeded.Should().BeFalse();
            ret.Reason.Should().Be(remote.Reason);
        }

        [Theory, SynthAutoData]
        public void CheckIfKeepingThrows(
            DirectoryPath local,
            GetResponse<string> remote,
            CheckOrCloneRepo sut)
        {
            sut.Delete.CheckIfKeeping(default, default).ThrowsForAnyArgs<NotImplementedException>();
            var ret = sut.Check(
                remote,
                local,
                CancellationToken.None);
            ret.Succeeded.Should().BeFalse();
        }

        [Theory, SynthAutoData]
        public void CloneCalledIfNotKeeping(
            DirectoryPath local,
            GetResponse<string> remote,
            CheckOrCloneRepo sut)
        {
            sut.Delete.CheckIfKeeping(default, default).Returns(false);
            var ret = sut.Check(
                remote,
                local,
                CancellationToken.None);
            sut.CloneRepo.Received(1).Clone(remote.Value, local);
        }

        [Theory, SynthAutoData]
        public void ReturnsRemoteAndLocal(
            DirectoryPath local,
            GetResponse<string> remote,
            DirectoryPath clonePath,
            CheckOrCloneRepo sut)
        {
            sut.Delete.CheckIfKeeping(default, default).Returns(false);
            sut.CloneRepo.Clone(default!, default).ReturnsForAnyArgs(clonePath);
            var ret = sut.Check(
                remote,
                local,
                CancellationToken.None);
            sut.CloneRepo.Received(1).Clone(remote.Value, local);
        }
    }
}