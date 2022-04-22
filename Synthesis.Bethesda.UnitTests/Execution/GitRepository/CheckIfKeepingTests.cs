using Xunit;
using FluentAssertions;
using LibGit2Sharp;
using Noggog;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Synthesis.Bethesda.Execution.GitRepository;
using Synthesis.Bethesda.UnitTests.AutoData;

namespace Synthesis.Bethesda.UnitTests.Execution.GitRepository;

public class CheckIfKeepingTests
{
    [Theory, SynthAutoData]
    public void LocalDirDoesNotExistReturnsFalse(
        DirectoryPath missingDir,
        GetResponse<string> remoteUrl,
        CheckIfKeeping sut)
    {
        sut.ShouldKeep(missingDir, remoteUrl)
            .Should().BeFalse();
    }
        
    [Theory, SynthAutoData]
    public void RemoteUrlFailedReturnsFalse(
        DirectoryPath existingDir,
        GetResponse<string> failedRemote,
        CheckIfKeeping sut)
    {
        sut.ShouldKeep(existingDir, failedRemote)
            .Should().BeFalse();
    }
        
    [Theory, SynthAutoData]
    public void PassesLocalDirToCheckout(
        DirectoryPath existingDir,
        GetResponse<string> remote,
        CheckIfKeeping sut)
    {
        sut.ShouldKeep(existingDir, remote);
        sut.RepoCheckouts.Received(1).Get(existingDir);
    }
        
    [Theory, SynthAutoData]
    public void PassesCheckoutToDesirability(
        DirectoryPath existingDir,
        GetResponse<string> remote,
        IRepositoryCheckout checkout,
        CheckIfKeeping sut)
    {
        sut.RepoCheckouts.Get(default).ReturnsForAnyArgs(checkout);
        sut.ShouldKeep(existingDir, remote);
        sut.IfRepositoryDesirable.Received(1).IsDesirable(checkout.Repository);
    }
        
    [Theory, SynthAutoData]
    public void UndesirableReturnsFalse(
        DirectoryPath existingDir,
        GetResponse<string> remote,
        CheckIfKeeping sut)
    {
        sut.IfRepositoryDesirable.IsDesirable(default!).ReturnsForAnyArgs(false);
        sut.ShouldKeep(existingDir, remote)
            .Should().BeFalse();
    }
        
    [Theory, SynthAutoData]
    public void SameRepositoryAddressReturnsTrue(
        DirectoryPath existingDir,
        GetResponse<string> remote,
        IRepositoryCheckout checkout,
        CheckIfKeeping sut)
    {
        sut.IfRepositoryDesirable.IsDesirable(default!).ReturnsForAnyArgs(true);
        sut.RepoCheckouts.Get(default).ReturnsForAnyArgs(checkout);
        checkout.Repository.MainRemoteUrl.Returns(remote.Value);
            
        sut.ShouldKeep(existingDir, remote)
            .Should().BeTrue();
    }
        
    [Theory, SynthAutoData]
    public void DifferentRepositoryAddressReturnsTrue(
        DirectoryPath existingDir,
        GetResponse<string> remote,
        IRepositoryCheckout checkout,
        string otherAddress,
        CheckIfKeeping sut)
    {
        sut.IfRepositoryDesirable.IsDesirable(default!).ReturnsForAnyArgs(true);
        checkout.Repository.MainRemoteUrl.Returns(otherAddress);
            
        sut.ShouldKeep(existingDir, remote)
            .Should().BeFalse();
    }
        
    [Theory, SynthAutoData]
    public void RepositoryNotFoundExceptionReturnsFalse(
        DirectoryPath existingDir,
        GetResponse<string> remote,
        CheckIfKeeping sut)
    {
        sut.IfRepositoryDesirable.IsDesirable(default!).ReturnsForAnyArgs(true);
        sut.RepoCheckouts.Get(default).ThrowsForAnyArgs<RepositoryNotFoundException>();
            
        sut.ShouldKeep(existingDir, remote)
            .Should().BeFalse();
    }
}