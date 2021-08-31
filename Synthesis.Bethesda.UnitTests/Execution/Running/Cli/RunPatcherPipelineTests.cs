using System;
using System.Threading;
using System.Threading.Tasks;
using Mutagen.Bethesda.Plugins;
using Noggog;
using NSubstitute;
using Synthesis.Bethesda.Execution.Groups;
using Synthesis.Bethesda.Execution.Patchers.Running;
using Synthesis.Bethesda.Execution.Running.Cli;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.Running.Cli
{
    public class RunPatcherPipelineTests
    {
        [Theory, SynthAutoData]
        public async Task PassesGetGroupRunnersToRun(
            IGroupRun[] groupRuns,
            CancellationToken cancel,
            RunPatcherPipeline sut)
        {
            sut.GetGroupRunners.Get(cancel).ReturnsForAnyArgs(groupRuns);
            await sut.Run(cancel);
            await sut.ExecuteRun.Received(1).Run(
                groupRuns, Arg.Any<CancellationToken>(), Arg.Any<DirectoryPath>(),
                Arg.Any<FilePath?>(), Arg.Any<PersistenceMode>(), Arg.Any<string?>());
        }
        
        [Theory, SynthAutoData]
        public async Task PassesTypicalSettings(
            CancellationToken cancel,
            RunPatcherPipeline sut)
        {
            sut.Instructions.PersistenceMode = PersistenceMode.None;
            await sut.Run(cancel);
            await sut.ExecuteRun.Received(1).Run(
                Arg.Any<IGroupRun[]>(),
                Arg.Any<CancellationToken>(),
                outputDir: sut.Instructions.OutputDirectory,
                sourcePath: sut.Instructions.SourcePath,
                persistenceMode: sut.Instructions.PersistenceMode.Value, 
                persistencePath: sut.Instructions.PersistencePath);
        }
        
        [Theory, SynthAutoData]
        public async Task NullPersistenceModeFallsBackToNone(
            CancellationToken cancel,
            RunPatcherPipeline sut)
        {
            sut.Instructions.PersistenceMode = null;
            await sut.Run(cancel);
            await sut.ExecuteRun.Received(1).Run(
                Arg.Any<IGroupRun[]>(), Arg.Any<CancellationToken>(), Arg.Any<DirectoryPath>(),
                Arg.Any<FilePath?>(), PersistenceMode.None, Arg.Any<string?>());
        }
    }
}