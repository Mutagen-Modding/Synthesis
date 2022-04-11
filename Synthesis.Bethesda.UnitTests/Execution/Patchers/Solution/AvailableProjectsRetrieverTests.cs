using System.IO.Abstractions;
using AutoFixture.Xunit2;
using FluentAssertions;
using Noggog;
using NSubstitute;
using Synthesis.Bethesda.Execution.Patchers.Solution;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.Patchers.Solution;

public class AvailableProjectsRetrieverTests
{
    [Theory, SynthAutoData(UseMockFileSystem: false)]
    public void PathDoesNotExistReturnsEmpty(
        [Frozen]IFileSystem fs,
        FilePath solutionPath,
        AvailableProjectsRetriever sut)
    {
        fs.File.Exists(default).ReturnsForAnyArgs(false);
        sut.Get(solutionPath)
            .Should().BeEmpty();
    }
}