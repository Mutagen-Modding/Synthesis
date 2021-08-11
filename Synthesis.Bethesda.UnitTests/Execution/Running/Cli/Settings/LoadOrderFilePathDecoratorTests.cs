using FluentAssertions;
using Noggog;
using NSubstitute;
using Synthesis.Bethesda.Execution.Running.Cli.Settings;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.Running.Cli.Settings
{
    public class LoadOrderFilePathDecoratorTests
    {
        [Theory, SynthAutoData]
        public void ReturnsInstructionsIfNotNull(
            FilePath path,
            LoadOrderFilePathDecorator sut)
        {
            sut.Instructions.LoadOrderFilePath = path;
            sut.Path.Should().Be(path);
        }
        
        [Theory, SynthAutoData]
        public void InstructionsNullReturnProvider(
            FilePath path,
            LoadOrderFilePathDecorator sut)
        {
            sut.Provider.Path.Returns(path);
            sut.Instructions.LoadOrderFilePath = default;
            sut.Path.Should().Be(path);
        }
    }
}