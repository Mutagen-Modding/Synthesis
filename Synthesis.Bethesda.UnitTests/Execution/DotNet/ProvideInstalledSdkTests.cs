using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Synthesis.Bethesda.Execution.DotNet;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.DotNet
{
    public class ProvideInstalledSdkTests
    {
        [Theory, SynthAutoData]
        public async Task Success(InstalledSdkProvider sut)
        {
            var version = "Version";
            sut.Query.Query(CancellationToken.None).Returns(
                new DotNetVersion(version, true));
            await sut.DotNetSdkInstalled
                .Do(x =>
                {
                    x.Acceptable.Should().BeTrue();
                    x.Version.Should().Be(version);
                });
        }
        
        [Theory, SynthAutoData]
        public async Task Fail(InstalledSdkProvider sut)
        {
            sut.Query.Query(CancellationToken.None).Returns(
                new DotNetVersion(string.Empty, false));
            await sut.DotNetSdkInstalled
                .Do(x =>
                {
                    x.Acceptable.Should().BeFalse();
                });
        }
        
        [Theory, SynthAutoData]
        public async Task Throws(InstalledSdkProvider sut)
        {
            sut.Query.Query(CancellationToken.None)
                .ThrowsForAnyArgs(_ => new Exception());
            await sut.DotNetSdkInstalled
                .Do(x =>
                {
                    x.Acceptable.Should().BeFalse();
                });
        }
    }
}