using FluentAssertions;
using Noggog;
using NSubstitute;
using Synthesis.Bethesda.Execution.Patchers.Solution;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.Patchers.Solution
{
    public class AvailableProjectsRetrieverTests
    {
        [Theory, SynthAutoData]
        public void PathDoesNotExistReturnsEmpty(
            FilePath solutionPath,
            AvailableProjectsRetriever sut)
        {
            sut.FileSystem.File.Exists(default).ReturnsForAnyArgs(false);
            sut.Get(solutionPath)
                .Should().BeEmpty();
        }
    }
}