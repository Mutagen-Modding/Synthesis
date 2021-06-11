using System;
using System.Reactive.Disposables;
using AutoFixture;
using FluentAssertions;
using LibGit2Sharp;
using Synthesis.Bethesda.Execution.GitRespository;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.GitRepository
{
    public class RepositoryCheckoutTests: RepoTestUtility, IClassFixture<Fixture>
    {
        private readonly Fixture _Fixture;

        public RepositoryCheckoutTests(Fixture fixture)
        {
            _Fixture = fixture;
        }
        
        [Fact]
        public void DoesCleanupCall()
        {
            var cleanup = new CancellationDisposable();
            new RepositoryCheckout(
                    _Fixture.Inject.Create<Lazy<Repository>>(),
                    cleanup)
                .Dispose();
            cleanup.IsDisposed.Should().BeTrue();
        }
    }
}