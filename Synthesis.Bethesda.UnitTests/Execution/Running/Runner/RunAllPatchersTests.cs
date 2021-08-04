using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Mutagen.Bethesda.Plugins;
using Noggog;
using NSubstitute;
using NSubstitute.Core;
using NSubstitute.ExceptionExtensions;
using Synthesis.Bethesda.Execution.Patchers.Running;
using Synthesis.Bethesda.Execution.Running.Runner;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.Running.Runner
{
    public class RunAllPatchersTests
    {
        [Theory, SynthAutoData]
        public async Task PassesEachPatcherToRunComponent(
            ModKey outputKey,
            (int Key, IPatcherRun Run)[] patchers,
            Task<Exception?>[] patcherPreps,
            CancellationToken cancellation,
            FilePath? sourcePath,
            string? persistencePath,
            RunAllPatchers sut)
        {
            await sut.Run(
                outputKey,
                patchers,
                patcherPreps,
                cancellation,
                sourcePath,
                persistencePath);

            await sut.RunAPatcher.ReceivedWithAnyArgs().Run(
                default, default!, default!,
                default!, default, default, default);

            for (int i = 0; i < patchers.Length; i++)
            {
                await sut.RunAPatcher.Received(1)
                    .Run(outputKey,
                        patchers[i].Key,
                        patchers[i].Run,
                        patcherPreps[i],
                        cancellation,
                        Arg.Any<FilePath?>(),
                        persistencePath);
            }
        }
        
        [Theory, SynthAutoData]
        public async Task PassesPreviousPathToNext(
            ModKey outputKey,
            ModPath return1,
            ModPath return2,
            CancellationToken cancellation,
            FilePath? sourcePath,
            string? persistencePath,
            RunAllPatchers sut)
        {
            (int Key, IPatcherRun Run)[] patchers = new[]
            {
                (1, Substitute.For<IPatcherRun>()),
                (2, Substitute.For<IPatcherRun>()),
            };
            Task<Exception?>[] patcherPreps = new Task<Exception?>[]
            {
                Task.FromResult<Exception?>(null),
                Task.FromResult<Exception?>(null),
            };
            
            sut.RunAPatcher.Run(
                    Arg.Any<ModKey>(),
                    1,
                    Arg.Any<IPatcherRun>(),
                    Arg.Any<Task<Exception?>>(), 
                    Arg.Any<CancellationToken>(),
                    Arg.Any<FilePath?>(),
                    Arg.Any<string?>())
                .Returns(return1);
            
            sut.RunAPatcher.Run(
                    Arg.Any<ModKey>(),
                    2,
                    Arg.Any<IPatcherRun>(),
                    Arg.Any<Task<Exception?>>(), 
                    Arg.Any<CancellationToken>(),
                    Arg.Any<FilePath?>(),
                    Arg.Any<string?>())
                .Returns(return2);
                
            await sut.Run(
                outputKey,
                patchers,
                patcherPreps,
                cancellation,
                sourcePath,
                persistencePath);

            await sut.RunAPatcher.Received(1)
                .Run(
                    Arg.Any<ModKey>(),
                    1,
                    Arg.Any<IPatcherRun>(),
                    Arg.Any<Task<Exception?>>(),
                    Arg.Any<CancellationToken>(),
                    sourcePath,
                    Arg.Any<string?>());

            await sut.RunAPatcher.Received(1)
                .Run(
                    Arg.Any<ModKey>(),
                    2,
                    Arg.Any<IPatcherRun>(),
                    Arg.Any<Task<Exception?>>(),
                    Arg.Any<CancellationToken>(),
                    return1,
                    Arg.Any<string?>());
        }
        
        [Theory, SynthAutoData]
        public async Task ReturnsFinalPatcherReturn(
            ModKey outputKey,
            FilePath return1,
            FilePath return2,
            CancellationToken cancellation,
            FilePath? sourcePath,
            string? persistencePath,
            RunAllPatchers sut)
        {
            (int Key, IPatcherRun Run)[] patchers = new[]
            {
                (1, Substitute.For<IPatcherRun>()),
                (2, Substitute.For<IPatcherRun>()),
            };
            Task<Exception?>[] patcherPreps = new Task<Exception?>[]
            {
                Task.FromResult<Exception?>(null),
                Task.FromResult<Exception?>(null),
            };
            
            sut.RunAPatcher.Run(
                    Arg.Any<ModKey>(),
                    1,
                    Arg.Any<IPatcherRun>(),
                    Arg.Any<Task<Exception?>>(), 
                    Arg.Any<CancellationToken>(),
                    Arg.Any<FilePath?>(),
                    Arg.Any<string?>())
                .Returns(return1);
            
            sut.RunAPatcher.Run(
                    Arg.Any<ModKey>(),
                    2,
                    Arg.Any<IPatcherRun>(),
                    Arg.Any<Task<Exception?>>(), 
                    Arg.Any<CancellationToken>(),
                    Arg.Any<FilePath?>(),
                    Arg.Any<string?>())
                .Returns(return2);

            (await sut.Run(
                    outputKey,
                    patchers,
                    patcherPreps,
                    cancellation,
                    sourcePath,
                    persistencePath))
                .Should().Be(return2);
        }
        
        [Theory, SynthAutoData]
        public async Task PatcherNullReturnDoesNotRunAnyMore(
            ModKey outputKey,
            ModPath return2,
            CancellationToken cancellation,
            FilePath? sourcePath,
            string? persistencePath,
            RunAllPatchers sut)
        {
            (int Key, IPatcherRun Run)[] patchers = new[]
            {
                (1, Substitute.For<IPatcherRun>()),
                (2, Substitute.For<IPatcherRun>()),
            };
            Task<Exception?>[] patcherPreps = new Task<Exception?>[]
            {
                Task.FromResult<Exception?>(null),
                Task.FromResult<Exception?>(null),
            };
            
            sut.RunAPatcher.Run(
                    Arg.Any<ModKey>(),
                    1,
                    Arg.Any<IPatcherRun>(),
                    Arg.Any<Task<Exception?>>(), 
                    Arg.Any<CancellationToken>(),
                    Arg.Any<FilePath?>(),
                    Arg.Any<string?>())
                .Returns(default(FilePath?));
            
            sut.RunAPatcher.Run(
                    Arg.Any<ModKey>(),
                    2,
                    Arg.Any<IPatcherRun>(),
                    Arg.Any<Task<Exception?>>(), 
                    Arg.Any<CancellationToken>(),
                    Arg.Any<FilePath?>(),
                    Arg.Any<string?>())
                .Returns(return2);

            await sut.Run(
                outputKey,
                patchers,
                patcherPreps,
                cancellation,
                sourcePath,
                persistencePath);

            await sut.RunAPatcher.DidNotReceive()
                .Run(
                    Arg.Any<ModKey>(),
                    2,
                    Arg.Any<IPatcherRun>(),
                    Arg.Any<Task<Exception?>>(),
                    Arg.Any<CancellationToken>(),
                    Arg.Any<FilePath?>(),
                    Arg.Any<string?>());
        }
        
        [Theory, SynthAutoData]
        public async Task ThrowingPatcherRethrows(
            ModKey outputKey,
            ModPath return2,
            CancellationToken cancellation,
            FilePath? sourcePath,
            string? persistencePath,
            RunAllPatchers sut)
        {
            (int Key, IPatcherRun Run)[] patchers = new[]
            {
                (1, Substitute.For<IPatcherRun>()),
                (2, Substitute.For<IPatcherRun>()),
            };
            Task<Exception?>[] patcherPreps = new Task<Exception?>[]
            {
                Task.FromResult<Exception?>(null),
                Task.FromResult<Exception?>(null),
            };

            sut.RunAPatcher.Run(
                    Arg.Any<ModKey>(),
                    1,
                    Arg.Any<IPatcherRun>(),
                    Arg.Any<Task<Exception?>>(),
                    Arg.Any<CancellationToken>(),
                    Arg.Any<FilePath?>(),
                    Arg.Any<string?>())
                .ThrowsForAnyArgs<NotImplementedException>();
            
            sut.RunAPatcher.Run(
                    Arg.Any<ModKey>(),
                    2,
                    Arg.Any<IPatcherRun>(),
                    Arg.Any<Task<Exception?>>(), 
                    Arg.Any<CancellationToken>(),
                    Arg.Any<FilePath?>(),
                    Arg.Any<string?>())
                .Returns(return2);

            await Assert.ThrowsAsync<NotImplementedException>(async () =>
            {
                await sut.Run(
                    outputKey,
                    patchers,
                    patcherPreps,
                    cancellation,
                    sourcePath,
                    persistencePath);
            });

            await sut.RunAPatcher.DidNotReceive()
                .Run(
                    Arg.Any<ModKey>(),
                    2,
                    Arg.Any<IPatcherRun>(),
                    Arg.Any<Task<Exception?>>(),
                    Arg.Any<CancellationToken>(),
                    Arg.Any<FilePath?>(),
                    Arg.Any<string?>());
        }
    }
}