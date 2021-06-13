using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using FluentAssertions;
using Noggog.Reactive;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Serilog;
using Synthesis.Bethesda.Execution.DotNet;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.DotNet
{
    public class ProvideInstalledSdkTests : IClassFixture<Fixture>
    {
        private readonly Fixture _Fixture;

        public ProvideInstalledSdkTests(Fixture fixture)
        {
            _Fixture = fixture;
        }
        
        [Fact]
        public async Task Success()
        {
            var version = "Version";
            var query = Substitute.For<IQueryInstalledSdk>();
            query.Query(CancellationToken.None).Returns(
                new DotNetVersion(version, true));
            var provide = new ProvideInstalledSdk(
                _Fixture.Inject.Create<ISchedulerProvider>(),
                query,
                _Fixture.Inject.Create<ILogger>());
            await provide.DotNetSdkInstalled
                .Do(x =>
                {
                    x.Acceptable.Should().BeTrue();
                    x.Version.Should().Be(version);
                });
        }
        
        [Fact]
        public async Task Fail()
        {
            var query = Substitute.For<IQueryInstalledSdk>();
            query.Query(CancellationToken.None).Returns(
                new DotNetVersion(string.Empty, false));
            var provide = new ProvideInstalledSdk(
                _Fixture.Inject.Create<ISchedulerProvider>(),
                query,
                _Fixture.Inject.Create<ILogger>());
            await provide.DotNetSdkInstalled
                .Do(x =>
                {
                    x.Acceptable.Should().BeFalse();
                });
        }
        
        [Fact]
        public async Task Throws()
        {
            var query = Substitute.For<IQueryInstalledSdk>();
            query.Query(CancellationToken.None)
                .ThrowsForAnyArgs(_ => new Exception());
            var provide = new ProvideInstalledSdk(
                _Fixture.Inject.Create<ISchedulerProvider>(),
                query,
                _Fixture.Inject.Create<ILogger>());
            await provide.DotNetSdkInstalled
                .Do(x =>
                {
                    x.Acceptable.Should().BeFalse();
                });
        }
    }
}