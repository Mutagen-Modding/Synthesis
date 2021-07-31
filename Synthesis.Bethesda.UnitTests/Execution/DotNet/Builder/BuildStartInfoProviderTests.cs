using System.Diagnostics;
using FluentAssertions;
using Noggog;
using NSubstitute;
using Synthesis.Bethesda.Execution.DotNet.Builder;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.DotNet.Builder
{
    public class BuildStartInfoProviderTests
    {
        [Theory, SynthAutoData]
        public void PassesBuildToConstructor(
            FilePath path,
            BuildStartInfoProvider sut)
        {
            sut.Construct(path);
            sut.StartConstructor.Received(1).Construct("build", path);
        }
        
        [Theory, SynthAutoData]
        public void ReturnsStartConstructorResults(
            ProcessStartInfo startInfo,
            FilePath path,
            BuildStartInfoProvider sut)
        {
            sut.StartConstructor.Construct(default!, default).ReturnsForAnyArgs(startInfo);
            sut.Construct(path)
                .Should().BeSameAs(startInfo);
        }
    }
}