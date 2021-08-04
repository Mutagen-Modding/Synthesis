using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;
using FluentAssertions;
using Mutagen.Bethesda.Plugins;
using Noggog;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.ReceivedExtensions;
using Synthesis.Bethesda.Execution.Patchers.Running;
using Synthesis.Bethesda.Execution.Running.Runner;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.Running.Runner
{
    public class ExecuteRunTests
    {
        [Theory, SynthAutoData]
        public async Task ResetsWorkingDirectory(
            ModPath outputPath,
            (int Key, IPatcherRun Run)[] patchers,
            CancellationToken cancellation,
            FilePath? sourcePath,
            PersistenceMode persistenceMode,
            string? persistencePath,
            ExecuteRun sut)
        {
            await sut.Run(
                outputPath,
                patchers,
                cancellation,
                sourcePath,
                persistenceMode,
                persistencePath);
            
            sut.ResetWorkingDirectory.Received(1).Reset();
        }
        
        [Theory, SynthAutoData]
        public async Task NoPatchersReturnsFalse(
            ModPath outputPath,
            CancellationToken cancellation,
            FilePath? sourcePath,
            PersistenceMode persistenceMode,
            string? persistencePath,
            ExecuteRun sut)
        {
            (int Key, IPatcherRun Run)[] patchers = Array.Empty<(int Key, IPatcherRun Run)>();
            var results = await sut.Run(
                outputPath,
                patchers,
                cancellation,
                sourcePath,
                persistenceMode,
                persistencePath);
            results.Should().BeFalse();
        }
        
        [Theory, SynthAutoData]
        public async Task CancellationThrows(
            ModPath outputPath,
            CancellationToken cancelled,
            FilePath? sourcePath,
            PersistenceMode persistenceMode,
            string? persistencePath,
            ExecuteRun sut)
        {
            (int Key, IPatcherRun Run)[] patchers = Array.Empty<(int Key, IPatcherRun Run)>();
            await Assert.ThrowsAsync<OperationCanceledException>(() => sut.Run(
                outputPath,
                patchers,
                cancelled,
                sourcePath,
                persistenceMode,
                persistencePath));
        }
        
        [Theory, SynthAutoData]
        public async Task EnsuresSourcePathExists(
            ModPath outputPath,
            (int Key, IPatcherRun Run)[] patchers,
            CancellationToken cancellation,
            FilePath? sourcePath,
            PersistenceMode persistenceMode,
            string? persistencePath,
            ExecuteRun sut)
        {
            await sut.Run(
                outputPath,
                patchers,
                cancellation,
                sourcePath,
                persistenceMode,
                persistencePath);
            
            sut.EnsureSourcePathExists.Received(1).Ensure(sourcePath);
        }
        
        [Theory, SynthAutoData]
        public async Task PreparesOverall(
            ModPath outputPath,
            (int Key, IPatcherRun Run)[] patchers,
            CancellationToken cancellation,
            FilePath? sourcePath,
            PersistenceMode persistenceMode,
            string? persistencePath,
            ExecuteRun sut)
        {
            await sut.Run(
                outputPath,
                patchers,
                cancellation,
                sourcePath,
                persistenceMode,
                persistencePath);
            
            await sut.OverallRunPreparer.Received(1).Prepare(outputPath, persistenceMode, persistencePath);
        }
        
        [Theory, SynthAutoData]
        public async Task PrepsPatchersForRun(
            ModPath outputPath,
            (int Key, IPatcherRun Run)[] patchers,
            CancellationToken cancellation,
            FilePath? sourcePath,
            PersistenceMode persistenceMode,
            string? persistencePath,
            ExecuteRun sut)
        {
            await sut.Run(
                outputPath,
                patchers,
                cancellation,
                sourcePath,
                persistenceMode,
                persistencePath);
            
            sut.PrepPatchersForRun.Received(1).PrepPatchers(patchers, cancellation);
        }
        
        [Theory, SynthAutoData]
        public async Task OverallPrepThrowingRethrowsBeforeRun(
            ModPath outputPath,
            (int Key, IPatcherRun Run)[] patchers,
            CancellationToken cancellation,
            FilePath? sourcePath,
            PersistenceMode persistenceMode,
            string? persistencePath,
            ExecuteRun sut)
        {
            sut.OverallRunPreparer.Prepare(default!)
                .ThrowsForAnyArgs<NotImplementedException>();

            await Assert.ThrowsAsync<NotImplementedException>(async () =>
            {
                await sut.Run(
                    outputPath,
                    patchers,
                    cancellation,
                    sourcePath,
                    persistenceMode,
                    persistencePath);
            });
            await sut.RunAllPatchers.DidNotReceiveWithAnyArgs()
                .Run(default, default!, default!, default);
        }
        
        [Theory, SynthAutoData]
        public async Task PatcherPrepThrowingRethrowsBeforeRun(
            ModPath outputPath,
            (int Key, IPatcherRun Run)[] patchers,
            CancellationToken cancellation,
            FilePath? sourcePath,
            PersistenceMode persistenceMode,
            string? persistencePath,
            ExecuteRun sut)
        {
            sut.PrepPatchersForRun.PrepPatchers(default!, default)
                .ThrowsForAnyArgs<NotImplementedException>();

            await Assert.ThrowsAsync<NotImplementedException>(async () =>
            {
                await sut.Run(
                    outputPath,
                    patchers,
                    cancellation,
                    sourcePath,
                    persistenceMode,
                    persistencePath);
            });
            await sut.RunAllPatchers.DidNotReceiveWithAnyArgs()
                .Run(default, default!, default!, default);
        }
        
        [Theory, SynthAutoData]
        public async Task PatcherPrepReturningExceptionDoesNotThrow(
            ModPath outputPath,
            (int Key, IPatcherRun Run)[] patchers,
            CancellationToken cancellation,
            FilePath? sourcePath,
            PersistenceMode persistenceMode,
            string? persistencePath,
            ExecuteRun sut)
        {
            sut.PrepPatchersForRun.PrepPatchers(default!, default)
                .ReturnsForAnyArgs(new Task<Exception?>[]
                {
                    Task.FromResult<Exception?>(new NotImplementedException())
                });

            await sut.Run(
                outputPath,
                patchers,
                cancellation,
                sourcePath,
                persistenceMode,
                persistencePath);
        }
        
        [Theory, SynthAutoData]
        public async Task PassesPreppedPatchersToRun(
            ModPath outputPath,
            (int Key, IPatcherRun Run)[] patchers,
            CancellationToken cancellation,
            FilePath? sourcePath,
            PersistenceMode persistenceMode,
            string? persistencePath,
            Task<Exception?>[] patcherPreps,
            ExecuteRun sut)
        {
            sut.PrepPatchersForRun.PrepPatchers(default!, default)
                .ReturnsForAnyArgs(patcherPreps);

            await sut.Run(
                outputPath,
                patchers,
                cancellation,
                sourcePath,
                persistenceMode,
                persistencePath);

            await sut.RunAllPatchers.Received(1)
                .Run(
                    Arg.Any<ModKey>(),
                    Arg.Any<(int Key, IPatcherRun Run)[]>(),
                    patcherPreps,
                    Arg.Any<CancellationToken>(),
                    Arg.Any<FilePath?>(),
                    Arg.Any<string?>());
        }
        
        [Theory, SynthAutoData]
        public async Task PassesOtherExpectedArgsToRun(
            ModPath outputPath,
            (int Key, IPatcherRun Run)[] patchers,
            CancellationToken cancellation,
            FilePath? sourcePath,
            PersistenceMode persistenceMode,
            string? persistencePath,
            ExecuteRun sut)
        {
            await sut.Run(
                outputPath,
                patchers,
                cancellation,
                sourcePath,
                persistenceMode,
                persistencePath);

            await sut.RunAllPatchers.Received(1)
                .Run(
                    outputPath.ModKey,
                    patchers,
                    Arg.Any<Task<Exception?>[]>(),
                    cancellation,
                    sourcePath,
                    persistencePath);
        }
        
        [Theory, SynthAutoData]
        public async Task NullRunReturnReturnsFalse(
            ModPath outputPath,
            (int Key, IPatcherRun Run)[] patchers,
            CancellationToken cancellation,
            FilePath? sourcePath,
            PersistenceMode persistenceMode,
            string? persistencePath,
            ExecuteRun sut)
        {
            sut.RunAllPatchers.Run(
                    Arg.Any<ModKey>(), Arg.Any<(int Key, IPatcherRun Run)[]>(), Arg.Any<Task<Exception?>[]>(), 
                    Arg.Any<CancellationToken>(), Arg.Any<FilePath?>(), Arg.Any<string?>())
                .ReturnsForAnyArgs(Task.FromResult(default(FilePath?)));

            (await sut.Run(
                    outputPath,
                    patchers,
                    cancellation,
                    sourcePath,
                    persistenceMode,
                    persistencePath))
                .Should().BeFalse();
        }
        
        [Theory, SynthAutoData]
        public async Task PassesRunResultToMove(
            ModPath outputPath,
            (int Key, IPatcherRun Run)[] patchers,
            CancellationToken cancellation,
            FilePath? sourcePath,
            PersistenceMode persistenceMode,
            string? persistencePath,
            ModPath finalPatch,
            ExecuteRun sut)
        {
            sut.RunAllPatchers.Run(
                    Arg.Any<ModKey>(), Arg.Any<(int Key, IPatcherRun Run)[]>(), Arg.Any<Task<Exception?>[]>(), 
                    Arg.Any<CancellationToken>(), Arg.Any<FilePath?>(), Arg.Any<string?>())
                .Returns(Task.FromResult<FilePath?>(finalPatch));

            await sut.Run(
                outputPath,
                patchers,
                cancellation,
                sourcePath,
                persistenceMode,
                persistencePath);
            
            sut.MoveFinalResults.Received(1)
                .Move(finalPatch, outputPath);
        }
        
        [Theory, SynthAutoData]
        public async Task PatchReturnedFromRunReturnsTrue(
            ModPath outputPath,
            (int Key, IPatcherRun Run)[] patchers,
            CancellationToken cancellation,
            FilePath? sourcePath,
            PersistenceMode persistenceMode,
            string? persistencePath,
            ModPath finalPatch,
            ExecuteRun sut)
        {
            sut.RunAllPatchers.Run(
                    Arg.Any<ModKey>(), Arg.Any<(int Key, IPatcherRun Run)[]>(), Arg.Any<Task<Exception?>[]>(), 
                    Arg.Any<CancellationToken>(), Arg.Any<FilePath?>(), Arg.Any<string?>())
                .ReturnsForAnyArgs(Task.FromResult<FilePath?>(finalPatch));

            (await sut.Run(
                    outputPath,
                    patchers,
                    cancellation,
                    sourcePath,
                    persistenceMode,
                    persistencePath))
                .Should().BeTrue();
        }
        
        [Theory, SynthAutoData]
        public async Task ResetBeforePreps(
            ModPath outputPath,
            (int Key, IPatcherRun Run)[] patchers,
            CancellationToken cancellation,
            FilePath? sourcePath,
            PersistenceMode persistenceMode,
            string? persistencePath,
            ExecuteRun sut)
        {
            await sut.Run(
                outputPath,
                patchers,
                cancellation,
                sourcePath,
                persistenceMode,
                persistencePath);
            
            Received.InOrder(() =>
            {
                sut.ResetWorkingDirectory.Reset();
                sut.OverallRunPreparer.Prepare(
                    Arg.Any<ModPath>(), Arg.Any<PersistenceMode>(), Arg.Any<string?>());
            });
            
            Received.InOrder(() =>
            {
                sut.ResetWorkingDirectory.Reset();
                sut.PrepPatchersForRun.PrepPatchers(
                    Arg.Any<IEnumerable<(int Key, IPatcherRun Run)>>(), Arg.Any<CancellationToken>());
            });
        }
        
        [Theory, SynthAutoData]
        public async Task EnsurePathExistsBeforePrep(
            ModPath outputPath,
            (int Key, IPatcherRun Run)[] patchers,
            CancellationToken cancellation,
            FilePath? sourcePath,
            PersistenceMode persistenceMode,
            string? persistencePath,
            ExecuteRun sut)
        {
            await sut.Run(
                outputPath,
                patchers,
                cancellation,
                sourcePath,
                persistenceMode,
                persistencePath);
            
            Received.InOrder(() =>
            {
                sut.EnsureSourcePathExists.Ensure(
                    Arg.Any<FilePath?>());
                sut.OverallRunPreparer.Prepare(
                    Arg.Any<ModPath>(), Arg.Any<PersistenceMode>(), Arg.Any<string?>());
            });
            
            Received.InOrder(() =>
            {
                sut.EnsureSourcePathExists.Ensure(
                    Arg.Any<FilePath?>());
                sut.PrepPatchersForRun.PrepPatchers(
                    Arg.Any<IEnumerable<(int Key, IPatcherRun Run)>>(), Arg.Any<CancellationToken>());
            });
        }
        
        [Theory, SynthAutoData]
        public async Task PrepBeforeRun(
            ModPath outputPath,
            (int Key, IPatcherRun Run)[] patchers,
            CancellationToken cancellation,
            FilePath? sourcePath,
            PersistenceMode persistenceMode,
            string? persistencePath,
            ExecuteRun sut)
        {
            await sut.Run(
                outputPath,
                patchers,
                cancellation,
                sourcePath,
                persistenceMode,
                persistencePath);
            
            Received.InOrder(() =>
            {
                sut.OverallRunPreparer.Prepare(
                    Arg.Any<ModPath>(), Arg.Any<PersistenceMode>(), Arg.Any<string?>());
                sut.RunAllPatchers.Run(
                    Arg.Any<ModKey>(), Arg.Any<(int Key, IPatcherRun Run)[]>(), Arg.Any<Task<Exception?>[]>(),
                    Arg.Any<CancellationToken>(), Arg.Any<FilePath?>(), Arg.Any<string?>());
            });
            
            Received.InOrder(() =>
            {
                sut.PrepPatchersForRun.PrepPatchers(
                    Arg.Any<IEnumerable<(int Key, IPatcherRun Run)>>(), Arg.Any<CancellationToken>());
                sut.RunAllPatchers.Run(
                    Arg.Any<ModKey>(), Arg.Any<(int Key, IPatcherRun Run)[]>(), Arg.Any<Task<Exception?>[]>(),
                    Arg.Any<CancellationToken>(), Arg.Any<FilePath?>(), Arg.Any<string?>());
            });
        }
        
        [Theory, SynthAutoData]
        public async Task RunBeforeMove(
            ModPath outputPath,
            (int Key, IPatcherRun Run)[] patchers,
            CancellationToken cancellation,
            FilePath? sourcePath,
            PersistenceMode persistenceMode,
            string? persistencePath,
            ExecuteRun sut)
        {
            await sut.Run(
                outputPath,
                patchers,
                cancellation,
                sourcePath,
                persistenceMode,
                persistencePath);
            
            Received.InOrder(() =>
            {
                sut.RunAllPatchers.Run(
                    Arg.Any<ModKey>(), Arg.Any<(int Key, IPatcherRun Run)[]>(), Arg.Any<Task<Exception?>[]>(),
                    Arg.Any<CancellationToken>(), Arg.Any<FilePath?>(), Arg.Any<string?>());
                sut.MoveFinalResults.Move(
                    Arg.Any<FilePath>(), Arg.Any<FilePath>());
            });
        }
    }
}