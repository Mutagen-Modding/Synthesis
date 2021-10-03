using System.IO;
using FluentAssertions;
using Noggog;
using NSubstitute;
using Synthesis.Bethesda.Execution.Pathing;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.Pathing
{
    public class ExtraDataPathProviderTests
    {
        [Theory, SynthAutoData]
        public void CombinesCurrentDirectoryAndData(
            DirectoryPath currentDir,
            ExtraDataPathProvider sut)
        {
            sut.CurrentDirectoryProvider.CurrentDirectory.Returns(currentDir);
            sut.Path.Should().Be(
                new DirectoryPath(
                    Path.Combine(currentDir, "Data")));
        }
    }
}