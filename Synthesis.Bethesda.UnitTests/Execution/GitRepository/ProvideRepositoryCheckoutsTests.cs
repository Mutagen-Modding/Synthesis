using Shouldly;
using Noggog;
using NSubstitute;
using Noggog.GitRepository;
using Synthesis.Bethesda.UnitTests.AutoData;

namespace Synthesis.Bethesda.UnitTests.Execution.GitRepository;

public class ProvideRepositoryCheckoutsTests
{
    [Theory, SynthAutoData]
    public void PassesPathToFactory(
        DirectoryPath local,
        ProvideRepositoryCheckouts sut)
    {
        using var checkout = sut.Get(local);
        var repo = checkout.Repository;
        sut.RepositoryFactory.Received(1).Get(local);
    }
        
    [Theory, SynthAutoData]
    public void ReturnsFactoryResults(
        DirectoryPath local,
        IGitRepository repo,
        ProvideRepositoryCheckouts sut)
    {
        sut.RepositoryFactory.Get(local).Returns(repo);
        using var checkout = sut.Get(local);
        checkout.Repository.ShouldBeSameAs(repo);
    }
        
    [Theory, SynthAutoData]
    public void CheckoutIsLazy(
        DirectoryPath local,
        ProvideRepositoryCheckouts sut)
    {
        using var checkout = sut.Get(local);
        sut.RepositoryFactory.DidNotReceiveWithAnyArgs().Get(default);
    }

    [Theory, SynthAutoData]
    public void CleanShutdown(
        DirectoryPath local,
        ProvideRepositoryCheckouts sut)
    {
        sut.Get(local).Dispose();
        sut.Dispose();
        sut.IsShutdownRequested.ShouldBeTrue();
        sut.IsShutdown.ShouldBeTrue();
    }

    [Theory, SynthAutoData]
    public async Task BlockedShutdown(
        DirectoryPath local,
        ProvideRepositoryCheckouts sut)
    {
        var checkout = sut.Get(local);
        var waited = false;
        var t = Task.Run(async () =>
        {
            await Task.Delay(500);
            sut.IsShutdownRequested.ShouldBeTrue();
            waited = true;
            checkout.Dispose();
        });
        sut.Dispose();
        waited.ShouldBeTrue();
        await t;
        sut.IsShutdown.ShouldBeTrue();
    }

    [Theory, SynthAutoData]
    public async Task RequestAfterShutdownThrows(
        DirectoryPath dir,
        ProvideRepositoryCheckouts sut)
    {
        sut.Dispose();
        Assert.Throws<InvalidOperationException>(() =>
        {
            sut.Get(dir);
        });
    }
}