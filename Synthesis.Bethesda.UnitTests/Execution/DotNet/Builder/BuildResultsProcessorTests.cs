using System.Collections.Generic;
using System.Threading;
using FluentAssertions;
using Noggog;
using NSubstitute;
using Synthesis.Bethesda.Execution.DotNet.Builder;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.DotNet.Builder
{
    public class BuildResultsProcessorTests
    {
        [Theory, SynthAutoData]
        public void TrimTargetPathFromFirstError(
            FilePath targetPath,
            IBuildOutputAccumulator accumulator,
            CancellationToken cancel,
            BuildResultsProcessor sut)
        {
            accumulator.FirstError.Returns($"{targetPath}{BuildResultsProcessor.TargetPathSuffix}End");
            sut.GetResults(targetPath, cancel, accumulator)
                .Reason.Should().Be("End");
        }
        
        [Theory, SynthAutoData]
        public void FirstErrorNullReturnsUnknownWithOutput(
            FilePath targetPath,
            IBuildOutputAccumulator accumulator,
            CancellationToken cancel,
            string output,
            BuildResultsProcessor sut)
        {
            accumulator.Output.ReturnsForAnyArgs(new List<string>() {output});
            accumulator.FirstError.Returns(default(string?));
            var results = sut.GetResults(targetPath, cancel, accumulator);
            results.Reason.Should().StartWith("Unknown Error");
            results.Reason.Should().Contain(output);
        }
        
        [Theory, SynthAutoData]
        public void FirstErrorNullAndCancelledReturnsCancelled(
            FilePath targetPath,
            IBuildOutputAccumulator accumulator,
            CancellationToken cancelled,
            BuildResultsProcessor sut)
        {
            accumulator.FirstError.Returns(default(string?));
            var results = sut.GetResults(targetPath, cancelled, accumulator);
            results.Reason.Should().Contain("Cancel");
        }
    }
}