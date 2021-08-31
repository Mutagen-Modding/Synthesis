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
using Synthesis.Bethesda.Execution.Groups;
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
            IGroupRun[] groups,
            CancellationToken cancellation,
            DirectoryPath outputDir,
            FilePath? sourcePath,
            PersistenceMode persistenceMode,
            string? persistencePath,
            ExecuteRun sut)
        {
            await sut.Run(
                groups,
                cancellation,
                outputDir,
                sourcePath,
                persistenceMode,
                persistencePath);
            
            sut.ResetWorkingDirectory.Received(1).Reset();
        }
        
        [Theory, SynthAutoData]
        public async Task NoPatchersShortCircuits(
            CancellationToken cancellation,
            FilePath? sourcePath,
            DirectoryPath outputDir,
            PersistenceMode persistenceMode,
            string? persistencePath,
            ExecuteRun sut)
        {
            IGroupRun[] groups = Array.Empty<IGroupRun>();
            await sut.Run(
                groups,
                cancellation,
                outputDir,
                sourcePath,
                persistenceMode,
                persistencePath);
            sut.EnsureSourcePathExists.DidNotReceiveWithAnyArgs().Ensure(default);
            await sut.RunAllGroups.DidNotReceiveWithAnyArgs().Run(
                default!, default, default);
        }
        
        [Theory, SynthAutoData]
        public async Task CancellationThrows(
            CancellationToken cancelled,
            FilePath? sourcePath,
            DirectoryPath outputDir,
            PersistenceMode persistenceMode,
            string? persistencePath,
            ExecuteRun sut)
        {
            IGroupRun[] groups = Array.Empty<IGroupRun>();
            await Assert.ThrowsAsync<OperationCanceledException>(() => sut.Run(
                groups,
                cancelled,
                outputDir,
                sourcePath,
                persistenceMode,
                persistencePath));
        }
        
        [Theory, SynthAutoData]
        public async Task EnsuresSourcePathExists(
            IGroupRun[] groups,
            CancellationToken cancellation,
            DirectoryPath outputDir,
            FilePath? sourcePath,
            PersistenceMode persistenceMode,
            string? persistencePath,
            ExecuteRun sut)
        {
            await sut.Run(
                groups,
                cancellation,
                outputDir,
                sourcePath,
                persistenceMode,
                persistencePath);
            
            sut.EnsureSourcePathExists.Received(1).Ensure(sourcePath);
        }
        
        [Theory, SynthAutoData]
        public async Task ResetBeforeEnsure(
            IGroupRun[] groups,
            CancellationToken cancellation,
            DirectoryPath outputDir,
            FilePath? sourcePath,
            PersistenceMode persistenceMode,
            string? persistencePath,
            ExecuteRun sut)
        {
            await sut.Run(
                groups,
                cancellation,
                outputDir,
                sourcePath,
                persistenceMode,
                persistencePath);
            
            Received.InOrder(() =>
            {
                sut.ResetWorkingDirectory.Reset();
                sut.EnsureSourcePathExists.Ensure(
                    Arg.Any<FilePath?>());
            });
        }
        
        [Theory, SynthAutoData]
        public async Task EnsurePathExistsBeforeRun(
            IGroupRun[] groups,
            CancellationToken cancellation,
            DirectoryPath outputDir,
            FilePath? sourcePath,
            PersistenceMode persistenceMode,
            string? persistencePath,
            ExecuteRun sut)
        {
            await sut.Run(
                groups,
                cancellation,
                outputDir,
                sourcePath,
                persistenceMode,
                persistencePath);
            
            Received.InOrder(() =>
            {
                sut.EnsureSourcePathExists.Ensure(
                    Arg.Any<FilePath?>());
                sut.RunAllGroups.Run(
                    Arg.Any<IGroupRun[]>(), Arg.Any<CancellationToken>(), Arg.Any<DirectoryPath>(), 
                    Arg.Any<FilePath?>(), Arg.Any<PersistenceMode>(), Arg.Any<string?>());
            });
        }
    }
}