using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Mutagen.Bethesda.Plugins;
using Noggog;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Synthesis.Bethesda.Execution.Patchers.Running;
using Synthesis.Bethesda.Execution.Running.Runner;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.Running.Runner
{
    public class RunAPatcherTests
    {
        [Theory, SynthAutoData]
        public async Task PrepExceptionReturnsNull(
            ModKey outputKey,
            int key,
            IPatcherRun patcher,
            CancellationToken cancellation,
            FilePath? sourcePath,
            string? persistencePath,
            RunAPatcher sut)
        {
            (await sut.Run(
                    outputKey,
                    key,
                    patcher,
                    Task.FromResult<Exception?>(new NotImplementedException()),
                    cancellation,
                    sourcePath,
                    persistencePath))
                .Should().BeNull();
        }
        
        [Theory, SynthAutoData]
        public async Task CancelledReturnsNull(
            ModKey outputKey,
            int key,
            IPatcherRun patcher,
            CancellationToken cancelled,
            FilePath? sourcePath,
            string? persistencePath,
            RunAPatcher sut)
        {
            (await sut.Run(
                    outputKey,
                    key,
                    patcher,
                    Task.FromResult<Exception?>(new NotImplementedException()),
                    cancelled,
                    sourcePath,
                    persistencePath))
                .Should().BeNull();
        }
        
        [Theory, SynthAutoData]
        public async Task RetrievesArgs(
            ModKey outputKey,
            int key,
            IPatcherRun patcher,
            CancellationToken cancellation,
            FilePath? sourcePath,
            string? persistencePath,
            RunSynthesisPatcher args,
            RunAPatcher sut)
        {
            sut.GetRunArgs.GetArgs(default!, default, default, default, default)
                .ReturnsForAnyArgs(args);
            await sut.Run(
                outputKey,
                key,
                patcher,
                Task.FromResult<Exception?>(null),
                cancellation,
                sourcePath,
                persistencePath);
            sut.GetRunArgs.Received(1).GetArgs(
                patcher,
                key,
                outputKey,
                sourcePath,
                persistencePath);
        }
        
        [Theory, SynthAutoData]
        public async Task PassesArgsToRun(
            ModKey outputKey,
            int key,
            IPatcherRun patcher,
            CancellationToken cancellation,
            FilePath? sourcePath,
            ModPath outputPath,
            string? persistencePath,
            RunSynthesisPatcher args,
            RunAPatcher sut)
        {
            args.OutputPath = outputPath;
            sut.GetRunArgs.GetArgs(default!, default, default, default, default)
                .ReturnsForAnyArgs(args);
            await sut.Run(
                outputKey,
                key,
                patcher,
                Task.FromResult<Exception?>(null),
                cancellation,
                sourcePath,
                persistencePath);
            await patcher.Received(1).Run(args, cancellation);
        }
        
        [Theory, SynthAutoData]
        public async Task GetArgsThrowsReturnsNull(
            ModKey outputKey,
            int key,
            IPatcherRun patcher,
            CancellationToken cancellation,
            FilePath? sourcePath,
            string? persistencePath,
            RunAPatcher sut)
        {
            sut.GetRunArgs.GetArgs(default!, default, default, default, default)
                .ThrowsForAnyArgs<NotImplementedException>();
            (await sut.Run(
                outputKey,
                key,
                patcher,
                Task.FromResult<Exception?>(null),
                cancellation,
                sourcePath,
                persistencePath)).Should().BeNull();
        }
        
        [Theory, SynthAutoData]
        public async Task RunThrowsReturnsNull(
            ModKey outputKey,
            int key,
            IPatcherRun patcher,
            CancellationToken cancellation,
            FilePath? sourcePath,
            string? persistencePath,
            RunAPatcher sut)
        {
            patcher.Run(default!, default)
                .ThrowsForAnyArgs<NotImplementedException>();
            (await sut.Run(
                outputKey,
                key,
                patcher,
                Task.FromResult<Exception?>(null),
                cancellation,
                sourcePath,
                persistencePath)).Should().BeNull();
        }

        [Theory, SynthAutoData]
        public async Task PassesArgsToFinalize(
            ModKey outputKey,
            int key,
            IPatcherRun patcher,
            CancellationToken cancellation,
            FilePath? sourcePath,
            string? persistencePath,
            RunSynthesisPatcher args,
            FilePath outputPath,
            RunAPatcher sut)
        {
            sut.GetRunArgs.GetArgs(default!, default, default, default, default)
                .ReturnsForAnyArgs(args);
            args.OutputPath = outputPath;
            await sut.Run(
                outputKey,
                key,
                patcher,
                Task.FromResult<Exception?>(null),
                cancellation,
                sourcePath,
                persistencePath);
            sut.FinalizePatcherRun.Received(1)
                .Finalize(patcher, key, args.OutputPath);
        }

        [Theory, SynthAutoData]
        public async Task ReturnsFinalizedResults(
            ModKey outputKey,
            int key,
            IPatcherRun patcher,
            CancellationToken cancellation,
            FilePath? sourcePath,
            string? persistencePath,
            FilePath ret,
            RunAPatcher sut)
        {
            sut.FinalizePatcherRun.Finalize(default!, default, default)
                .ReturnsForAnyArgs(ret);
            (await sut.Run(
                    outputKey,
                    key,
                    patcher,
                    Task.FromResult<Exception?>(null),
                    cancellation,
                    sourcePath,
                    persistencePath))
                .Should().Be(ret);
        }
    }
}