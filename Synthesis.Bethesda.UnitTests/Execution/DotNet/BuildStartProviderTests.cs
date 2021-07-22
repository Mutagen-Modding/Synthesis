using System.Diagnostics;
using FluentAssertions;
using Noggog;
using NSubstitute;
using Synthesis.Bethesda.Execution.DotNet;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.DotNet
{
    public class BuildStartProviderTests
    {
        [Theory, SynthAutoData]
        public void PassesBuildToConstructor(
            FilePath path,
            BuildStartProvider sut)
        {
            sut.Construct(path);
            sut.StartConstructor.Received(1).Construct("build", path);
        }
        
        [Theory, SynthAutoData]
        public void ReturnsStartConstructorResults(
            ProcessStartInfo startInfo,
            FilePath path,
            BuildStartProvider sut)
        {
            sut.StartConstructor.Construct(default!, default).ReturnsForAnyArgs(startInfo);
            sut.Construct(path)
                .Should().BeSameAs(startInfo);
        }
    }
}