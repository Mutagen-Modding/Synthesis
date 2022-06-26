using FluentAssertions;
using Noggog;
using NSubstitute;
using Synthesis.Bethesda.Execution.Patchers.Solution;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.Patchers.Solution;

public class DefaultDataPathProviderTests
{
    [Theory, SynthAutoData]
    public void PathReturnProjDirAndData(
        DefaultDataPathProvider sut)
    {
        sut.PathToProjProvider.Path.Returns(new FilePath("C:/Dir/Proj.csproj"));
        sut.Path.Should().Be(new DirectoryPath("C:/Dir/Data"));
    }
}