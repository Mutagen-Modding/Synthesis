using FluentAssertions;
using Noggog;
using NSubstitute;
using Synthesis.Bethesda.Execution.DotNet.Builder;
using Synthesis.Bethesda.Execution.DotNet.Builder.Transient;
using Synthesis.Bethesda.UnitTests.AutoData;

namespace Synthesis.Bethesda.UnitTests.Execution.DotNet.Builder;

public class BuildResultsProcessorTests
{
    [Theory, SynthAutoData]
    public void TrimTargetPathFromFirstError(
        FilePath targetPath,
        int intResult,
        IBuildOutputAccumulator accumulator,
        CancellationToken cancel,
        BuildResultsProcessor sut)
    {
        accumulator.FirstError.Returns($"{targetPath}{BuildResultsProcessor.TargetPathSuffix}End");
        sut.GetResults(targetPath, intResult, cancel, accumulator)
            .Reason.Should().Be("End");
    }
        
    [Theory, SynthAutoData]
    public void FirstErrorNullReturnsUnknownWithOutput(
        FilePath targetPath,
        int intResult,
        IBuildOutputAccumulator accumulator,
        CancellationToken cancel,
        string output,
        BuildResultsProcessor sut)
    {
        accumulator.Output.ReturnsForAnyArgs(new List<string>() {output});
        accumulator.FirstError.Returns(default(string?));
        var results = sut.GetResults(targetPath, intResult, cancel, accumulator);
        results.Reason.Should().StartWith("Unknown Error");
        results.Reason.Should().Contain(output);
    }
        
    [Theory, SynthAutoData]
    public void FirstErrorNullAndCancelledReturnsCancelled(
        FilePath targetPath,
        int intResult,
        IBuildOutputAccumulator accumulator,
        CancellationToken cancelled,
        BuildResultsProcessor sut)
    {
        accumulator.FirstError.Returns(default(string?));
        var results = sut.GetResults(targetPath, intResult, cancelled, accumulator);
        results.Reason.Should().Contain("Cancel");
    }
}