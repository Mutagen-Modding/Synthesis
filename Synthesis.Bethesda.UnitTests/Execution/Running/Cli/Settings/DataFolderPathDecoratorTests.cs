using FluentAssertions;
using Noggog;
using NSubstitute;
using Synthesis.Bethesda.Execution.Running.Cli.Settings;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.Running.Cli.Settings
{
    public class DataFolderPathDecoratorTests
    {
        [Theory, SynthAutoData]
        public void ReturnsInstructionsIfNotNull(
            DirectoryPath path,
            DataFolderPathDecorator sut)
        {
            sut.Instructions.DataFolderPath = path;
            sut.Path.Should().Be(path);
        }
        
        [Theory, SynthAutoData]
        public void InstructionsNullReturnDataDirectoryProvider(
            DirectoryPath path,
            DataFolderPathDecorator sut)
        {
            sut.DataDirectoryProvider.Path.Returns(path);
            sut.Instructions.DataFolderPath = default;
            sut.Path.Should().Be(path);
        }
    }
}