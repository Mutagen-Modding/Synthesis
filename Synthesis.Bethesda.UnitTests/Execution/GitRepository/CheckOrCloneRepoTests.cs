using System;
using System.Threading;
using AutoFixture;
using FluentAssertions;
using Noggog;
using NSubstitute;
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
            ret.Failed.Should().BeTrue();
            ret.Reason.Should().Be(remote.Reason);
        }
    }
}