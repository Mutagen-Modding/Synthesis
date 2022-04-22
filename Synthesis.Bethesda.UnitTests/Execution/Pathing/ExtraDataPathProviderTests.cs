using FluentAssertions;
using Noggog;
using Synthesis.Bethesda.Execution.Pathing;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.Pathing;

public class ExtraDataPathProviderTests
{
    [Theory, SynthAutoData]
    public void CombinesCurrentDirectoryAndData(
        ExtraDataPathProvider sut)
    {
        sut.Path.Should().Be(
            new DirectoryPath(
                Path.Combine(sut.CurrentDirectoryProvider.CurrentDirectory, "Data")));
    }
}