using System;
using System.Collections.Generic;
using System.Linq;
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
            IPatcherRun[] patchers,
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
            IPatcherRun[] patchers = Array.Empty<IPatcherRun>();
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
            IPatcherRun[] patchers = Array.Empty<IPatcherRun>();
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
            IPatcherRun[] patchers,
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
            IPatcherRun[] patchers,
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
            IPatcherRun[] patchers,
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
            IPatcherRun[] patchers,
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
            IPatcherRun[] patchers,
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
            IPatcherRun[] patchers,
            CancellationToken cancellation,
            FilePath? sourcePath,
            PersistenceMode persistenceMode,
            string? persistencePath,
            ExecuteRun sut)
        {
            sut.PrepPatchersForRun.PrepPatchers(default!, default)
                .ReturnsForAnyArgs(new PatcherPrepBundle[]
                {
                    new PatcherPrepBundle(
                        patchers[0],
                        Task.FromResult<Exception?>(new NotImplementedException()))
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
            IPatcherRun[] patchers,
            CancellationToken cancellation,
            FilePath? sourcePath,
            PersistenceMode persistenceMode,
            string? persistencePath,
            PatcherPrepBundle[] patcherPreps,
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
                    patcherPreps,
                    Arg.Any<CancellationToken>(),
                    Arg.Any<FilePath?>(),
                    Arg.Any<string?>());
        }
        
        [Theory, SynthAutoData]
        public async Task NullRunReturnReturnsFalse(
            ModPath outputPath,
            IPatcherRun[] patchers,
            CancellationToken cancellation,
            FilePath? sourcePath,
            PersistenceMode persistenceMode,
            string? persistencePath,
            ExecuteRun sut)
        {
            sut.RunAllPatchers.Run(
                    Arg.Any<ModKey>(), Arg.Any<PatcherPrepBundle[]>(),
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
            IPatcherRun[] patchers,
            CancellationToken cancellation,
            FilePath? sourcePath,
            PersistenceMode persistenceMode,
            string? persistencePath,
            ModPath finalPatch,
            ExecuteRun sut)
        {
            sut.RunAllPatchers.Run(
                    Arg.Any<ModKey>(), Arg.Any<PatcherPrepBundle[]>(),
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
            IPatcherRun[] patchers,
            CancellationToken cancellation,
            FilePath? sourcePath,
            PersistenceMode persistenceMode,
            string? persistencePath,
            ModPath finalPatch,
            ExecuteRun sut)
        {
            sut.RunAllPatchers.Run(
                    Arg.Any<ModKey>(), Arg.Any<PatcherPrepBundle[]>(),
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
            IPatcherRun[] patchers,
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
                    Arg.Any<IEnumerable<IPatcherRun>>(), Arg.Any<CancellationToken>());
            });
        }
        
        [Theory, SynthAutoData]
        public async Task EnsurePathExistsBeforePrep(
            ModPath outputPath,
            IPatcherRun[] patchers,
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
                    Arg.Any<IEnumerable<IPatcherRun>>(), Arg.Any<CancellationToken>());
            });
        }
        
        [Theory, SynthAutoData]
        public async Task PrepBeforeRun(
            ModPath outputPath,
            IPatcherRun[] patchers,
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
                    Arg.Any<ModKey>(), Arg.Any<PatcherPrepBundle[]>(),
                    Arg.Any<CancellationToken>(), Arg.Any<FilePath?>(), Arg.Any<string?>());
            });
            
            Received.InOrder(() =>
            {
                sut.PrepPatchersForRun.PrepPatchers(
                    Arg.Any<IEnumerable<IPatcherRun>>(), Arg.Any<CancellationToken>());
                sut.RunAllPatchers.Run(
                    Arg.Any<ModKey>(), Arg.Any<PatcherPrepBundle[]>(),
                    Arg.Any<CancellationToken>(), Arg.Any<FilePath?>(), Arg.Any<string?>());
            });
        }
        
        [Theory, SynthAutoData]
        public async Task RunBeforeMove(
            ModPath outputPath,
            IPatcherRun[] patchers,
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
                    Arg.Any<ModKey>(), Arg.Any<PatcherPrepBundle[]>(),
                    Arg.Any<CancellationToken>(), Arg.Any<FilePath?>(), Arg.Any<string?>());
                sut.MoveFinalResults.Move(
                    Arg.Any<FilePath>(), Arg.Any<FilePath>());
            });
        }
    }
}