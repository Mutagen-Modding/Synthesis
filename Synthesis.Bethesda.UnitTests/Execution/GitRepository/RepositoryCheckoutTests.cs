using System.Reactive.Disposables;
using FluentAssertions;
using NSubstitute;
using Synthesis.Bethesda.Execution.GitRepository;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.GitRepository;

public class RepositoryCheckoutTests
{
    [Fact]
    public void DoesCleanupCall()
    {
        var cleanup = new CancellationDisposable();
        var repoMock = Substitute.For<IGitRepository>();
        new RepositoryCheckout(
                new Lazy<IGitRepository>(repoMock),
                cleanup)
            .Dispose();
        cleanup.IsDisposed.Should().BeTrue();
        repoMock.Received().Dispose();
    }
}