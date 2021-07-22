using FluentAssertions;
using Noggog;
using NSubstitute;
using Synthesis.Bethesda.Execution.DotNet;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.DotNet
{
    public class DotNetCommandStartConstructorTests
    {
        [Theory, SynthAutoData]
        public void ProcessStartTargetsGivenDotNetPath(
            string dotNetPath,
            DotNetCommandStartConstructor sut)
        {
            sut.DotNetPathProvider.Path.Returns(dotNetPath);
            sut.Construct(default!, default)
                .FileName.Should().Be(dotNetPath);
        }
        
        [Theory, SynthAutoData]
        public void PassesToConstructor(
            string command,
            FilePath path,
            string args,
            DotNetCommandStartConstructor sut)
        {
            sut.Construct(command, path, args);
            sut.Constructor.Received(1).Get(command, path, args);
        }
        
        [Theory, SynthAutoData]
        public void ConstructorResultsUsedAsArgs(
            string ret,
            DotNetCommandStartConstructor sut)
        {
            sut.Constructor.Get(default!, default).ReturnsForAnyArgs(ret);
            sut.Construct(default!, default)
                .Arguments.Should().Be(ret);
        }
    }
}