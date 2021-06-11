using System;
using System.Threading;
using AutoFixture;
using FluentAssertions;
using Noggog;
using NSubstitute;
using Synthesis.Bethesda.Execution.GitRespository;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.GitRepository
{
    public class CheckOrCloneRepoTests: IClassFixture<Fixture>
    {
        private readonly Fixture _Fixture;

        public CheckOrCloneRepoTests(Fixture fixture)
        {
            _Fixture = fixture;
        }
        
        [Fact]
        public void CallsCheckIfKeeping()
        {
            var check = Substitute.For<IDeleteOldRepo>();
            check.CheckIfKeeping(default, default).ReturnsForAnyArgs(true);
            var checkOrClone = new CheckOrCloneRepo(check);
            var remote = _Fixture.Inject.Create<GetResponse<string>>();
            var local = _Fixture.Inject.Create<DirectoryPath>();
            var ret = checkOrClone.Check(
                remote,
                local,
                _Fixture.Inject.Create<Action<string>>(),
                CancellationToken.None);
            check.Received().CheckIfKeeping(local, remote);
            ret.Succeeded.Should().BeTrue();
            ret.Value.Remote.Should().Be(remote.Value);
            ret.Value.Local.Should().Be(local);
        }

        [Fact]
        public void RemoteFailed()
        {
            var check = Substitute.For<IDeleteOldRepo>();
            check.CheckIfKeeping(default, default).ReturnsForAnyArgs(false);
            var checkOrClone = new CheckOrCloneRepo(check);
            var remote = GetResponse<string>.Fail("Fail string");
            var local = _Fixture.Inject.Create<DirectoryPath>();
            var ret = checkOrClone.Check(
                remote,
                local,
                _Fixture.Inject.Create<Action<string>>(),
                CancellationToken.None);
            ret.Failed.Should().BeTrue();
            ret.Reason.Should().Be(remote.Reason);
        }
    }
}