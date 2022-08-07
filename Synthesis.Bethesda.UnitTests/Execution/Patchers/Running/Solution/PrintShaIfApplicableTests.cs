using Noggog;
using NSubstitute;
using Noggog.GitRepository;
using Synthesis.Bethesda.Execution.Patchers.Running.Solution;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.Patchers.Running.Solution;

public class PrintShaIfApplicableTests
{
    [Theory, SynthAutoData]
    public void PassesRepositoryPathToIsValidCheck(
        DirectoryPath repoPath,
        PrintShaIfApplicable sut)
    {
        sut.PathToRepositoryProvider.Path.Returns(repoPath);
        sut.Print();
        sut.LocalRepoIsValid.Received(1).IsValidRepository(repoPath);
    }
         
    [Theory, SynthAutoData]
    public void NoCheckoutIfNotValid(
        PrintShaIfApplicable sut)
    {
        sut.LocalRepoIsValid.IsValidRepository(default).ReturnsForAnyArgs(false);
        sut.Print();
        sut.RepositoryCheckouts.DidNotReceiveWithAnyArgs().Get(default);
    }
         
    [Theory, SynthAutoData]
    public void PassesRepoPathToCheckoutIfValid(
        DirectoryPath repoPath,
        PrintShaIfApplicable sut)
    {
        sut.PathToRepositoryProvider.Path.Returns(repoPath);
        sut.LocalRepoIsValid.IsValidRepository(default).ReturnsForAnyArgs(true);
        sut.Print();
        sut.RepositoryCheckouts.Received(1).Get(repoPath);
    }
         
    [Theory, SynthAutoData]
    public void DisposesCheckout(
        IRepositoryCheckout checkout,
        PrintShaIfApplicable sut)
    {
        sut.RepositoryCheckouts.Get(default).ReturnsForAnyArgs(checkout);
        sut.LocalRepoIsValid.IsValidRepository(default).ReturnsForAnyArgs(true);
        sut.Print();
        checkout.Received(1).Dispose();
    }
         
    [Theory, SynthAutoData]
    public void AccessesSha(
        IRepositoryCheckout repo,
        PrintShaIfApplicable sut)
    {
        sut.RepositoryCheckouts.Get(default).ReturnsForAnyArgs(repo);
        sut.LocalRepoIsValid.IsValidRepository(default).ReturnsForAnyArgs(true);
        sut.Print();
        var sha = repo.Repository.Received(1).CurrentSha;
    }
}