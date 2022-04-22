using FluentAssertions;
using Noggog;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Synthesis.Bethesda.Execution.GitRepository;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.GitRepository;

public class CheckOrCloneRepoTests
{
    [Theory, SynthAutoData]
    public void CallsCheckIfKeeping(
        GetResponse<string> remote,
        DirectoryPath local,
        CheckOrCloneRepo sut)
    {
        sut.ShouldKeep.ShouldKeep(default, default).ReturnsForAnyArgs(true);
        var ret = sut.Check(remote, local, CancellationToken.None);
        sut.ShouldKeep.Received().ShouldKeep(local, remote);
        ret.Succeeded.Should().BeTrue();
        ret.Value.Remote.Should().Be(remote.Value);
        ret.Value.Local.Should().Be(local);
        sut.CloneRepo.DidNotReceiveWithAnyArgs().Clone(default!, default);
    }

    [Theory, SynthAutoData]
    public void RemoteFailed(
        DirectoryPath local,
        GetResponse<string> failedRemote,
        CheckOrCloneRepo sut)
    {
        sut.ShouldKeep.ShouldKeep(default, default).ReturnsForAnyArgs(false);
        var ret = sut.Check(failedRemote, local, CancellationToken.None);
        ret.Succeeded.Should().BeFalse();
        ret.Reason.Should().Be(failedRemote.Reason);
    }

    [Theory, SynthAutoData]
    public void DeleteCalledIfRemoteFailed(
        DirectoryPath local,
        GetResponse<string> failedRemote,
        CheckOrCloneRepo sut)
    {
        sut.ShouldKeep.ShouldKeep(default, default).ReturnsForAnyArgs(false);
        sut.Check(failedRemote, local, CancellationToken.None);
        sut.DeleteEntireDirectory.Received(1).DeleteEntireFolder(
            local, deleteFolderItself: true, disableReadOnly: true);
    }

    [Theory, SynthAutoData]
    public void CheckIfKeepingThrows(
        DirectoryPath local,
        GetResponse<string> remote,
        CheckOrCloneRepo sut)
    {
        sut.ShouldKeep.ShouldKeep(default, default).ThrowsForAnyArgs<NotImplementedException>();
        var ret = sut.Check(remote, local, CancellationToken.None);
        ret.Succeeded.Should().BeFalse();
    }

    [Theory, SynthAutoData]
    public void DeleteCalledIfNotKeeping(
        DirectoryPath local,
        GetResponse<string> remote,
        CheckOrCloneRepo sut)
    {
        sut.ShouldKeep.ShouldKeep(default, default).Returns(false);
        sut.Check(remote, local, CancellationToken.None);
        sut.DeleteEntireDirectory.Received(1).DeleteEntireFolder(
            local, disableReadOnly: true, deleteFolderItself: true);
    }

    [Theory, SynthAutoData]
    public void CloneCalledIfNotKeeping(
        DirectoryPath local,
        GetResponse<string> remote,
        CheckOrCloneRepo sut)
    {
        sut.ShouldKeep.ShouldKeep(default, default).Returns(false);
        sut.Check(remote, local, CancellationToken.None);
        sut.CloneRepo.Received(1).Clone(remote.Value, local);
    }

    [Theory, SynthAutoData]
    public void DeleteCalledBeforeReclone(
        DirectoryPath local,
        GetResponse<string> remote,
        CheckOrCloneRepo sut)
    {
        sut.ShouldKeep.ShouldKeep(default, default).Returns(false);
        sut.Check(remote, local, CancellationToken.None);
        Received.InOrder(() =>
        {
            sut.DeleteEntireDirectory.DeleteEntireFolder(
                Arg.Any<DirectoryPath>(),
                Arg.Any<bool>(),
                Arg.Any<bool>());
            sut.CloneRepo.Clone(
                Arg.Any<string>(), 
                Arg.Any<DirectoryPath>());
        });
    }

    [Theory, SynthAutoData]
    public void ReturnsRemoteAndLocal(
        DirectoryPath local,
        GetResponse<string> remote,
        DirectoryPath clonePath,
        CheckOrCloneRepo sut)
    {
        sut.ShouldKeep.ShouldKeep(default, default).Returns(false);
        sut.CloneRepo.Clone(default!, default).ReturnsForAnyArgs(clonePath);
        sut.Check(
            remote,
            local,
            CancellationToken.None);
        sut.CloneRepo.Received(1).Clone(remote.Value, local);
    }
}