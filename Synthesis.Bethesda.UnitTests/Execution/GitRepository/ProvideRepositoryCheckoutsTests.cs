using System;
using System.Threading.Tasks;
using AutoFixture;
using FluentAssertions;
using Noggog;
using NSubstitute;
using Serilog;
using Synthesis.Bethesda.Execution.GitRepository;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

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
        checkout.Repository.Should().BeSameAs(repo);
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
        sut.IsShutdownRequested.Should().BeTrue();
        sut.IsShutdown.Should().BeTrue();
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
            sut.IsShutdownRequested.Should().BeTrue();
            waited = true;
            checkout.Dispose();
        });
        sut.Dispose();
        waited.Should().BeTrue();
        await t;
        sut.IsShutdown.Should().BeTrue();
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