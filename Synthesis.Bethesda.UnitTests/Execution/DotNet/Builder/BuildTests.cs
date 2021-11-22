using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using FluentAssertions;
using Noggog;
using Noggog.Testing.TestClassData;
using NSubstitute;
using Synthesis.Bethesda.Execution.DotNet.Builder;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.DotNet.Builder
{
    public class BuildTests
    {
        [Theory, SynthAutoData]
        public async Task PassesPathNameToStartProvider(
            FilePath targetPath,
            CancellationToken cancel,
            Build sut)
        {
            await sut.Compile(targetPath, cancel);
            sut.BuildStartInfoProvider.Received(1).Construct(targetPath.Name.ToString());
        }
        
        [Theory, SynthAutoData]
        public async Task StartInfoWorkingDirectorySetToPathDirectory(
            FilePath targetPath,
            CancellationToken cancel,
            ProcessStartInfo start,
            Build sut)
        {
            sut.BuildStartInfoProvider.Construct(default).ReturnsForAnyArgs(start);
            await sut.Compile(targetPath, cancel);
            start.WorkingDirectory.Should().Be(targetPath.Directory);
        }
        
        [Theory, SynthAutoData]
        public async Task RunnerSuccessReturnsSuccess(
            FilePath targetPath,
            CancellationToken cancel,
            Build sut)
        {
            sut.ProcessRunner.RunWithCallback(default!, default!, default!, default)
                .ReturnsForAnyArgs(0);
            var result = await sut.Compile(targetPath, cancel);
            result.Should().Be(ErrorResponse.Success);
        }
        
        [Theory, SynthAutoData]
        public async Task RunnerSuccessDoesNotCallResultProcessor(
            FilePath targetPath,
            CancellationToken cancel,
            Build sut)
        {
            sut.ProcessRunner.RunWithCallback(default!, default!, default!, default)
                .ReturnsForAnyArgs(0);
            await sut.Compile(targetPath, cancel);
            sut.ResultsProcessor.DidNotReceiveWithAnyArgs().GetResults(default, default, default, default!);
        }
        
        [Theory, SynthAutoData]
        public async Task RunnerFailureCallsResultBuilder(
            FilePath targetPath,
            CancellationToken cancel,
            [Frozen]IBuildOutputAccumulator accumulator,
            Build sut)
        {
            sut.ProcessRunner.RunWithCallback(default!, default!, default!, default)
                .ReturnsForAnyArgs(-1);
            await sut.Compile(targetPath, cancel);
            sut.ResultsProcessor.Received(1).GetResults(targetPath, -1, cancel, accumulator);
        }

        public static IEnumerable<object[]> ErrorResponses => ErrorResponseSuccessFailData.Data;
        
        [Theory, SynthMemberData(nameof(ErrorResponses))]
        public async Task RunnerFailureReturnsResultsProcessor(
            ErrorResponse errorResponse,
            FilePath targetPath,
            CancellationToken cancel,
            Build sut)
        {
            sut.ProcessRunner.RunWithCallback(default!, default!, default!, default)
                .ReturnsForAnyArgs(-1);
            sut.ResultsProcessor.GetResults(default, default, default, default!)
                .ReturnsForAnyArgs(errorResponse);
            var result = await sut.Compile(targetPath, cancel);
            result.Should().Be(errorResponse);
        }
    }
}