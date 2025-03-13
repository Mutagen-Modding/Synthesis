using System.Reactive.Disposables;
using Shouldly;
using NSubstitute;
using Noggog.GitRepository;

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
        cleanup.IsDisposed.ShouldBeTrue();
        repoMock.Received().Dispose();
    }
}