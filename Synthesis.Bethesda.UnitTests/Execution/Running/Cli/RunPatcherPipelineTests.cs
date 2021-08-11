using System.Threading;
using System.Threading.Tasks;
using Mutagen.Bethesda.Plugins;
using Noggog;
using NSubstitute;
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
        public async Task PassesGetPatcherRunnersToRun(
            (int Key, IPatcherRun Run)[] patchers,
            CancellationToken cancel,
            RunPatcherPipeline sut)
        {
            sut.GetPatcherRunners.Get().ReturnsForAnyArgs(patchers);
            await sut.Run(cancel);
            await sut.ExecuteRun.Run(
                Arg.Any<ModPath>(), patchers, Arg.Any<CancellationToken>(),
                Arg.Any<FilePath?>(), Arg.Any<PersistenceMode>(), Arg.Any<string?>());
        }
        
        [Theory, SynthAutoData]
        public async Task PassesTypicalSettings(
            CancellationToken cancel,
            RunPatcherPipeline sut)
        {
            sut.Instructions.PersistenceMode = PersistenceMode.None;
            await sut.Run(cancel);
            await sut.ExecuteRun.Run(
                outputPath: sut.Instructions.OutputPath, 
                Arg.Any<(int Key, IPatcherRun Run)[]>(),
                Arg.Any<CancellationToken>(),
                sourcePath: sut.Instructions.SourcePath,
                persistenceMode: sut.Instructions.PersistenceMode.Value, 
                persistencePath: sut.Instructions.PersistencePath);
        }
        
        [Theory, SynthAutoData]
        public async Task NullPersistenceModeFallsBackToText(
            CancellationToken cancel,
            RunPatcherPipeline sut)
        {
            sut.Instructions.PersistenceMode = null;
            await sut.Run(cancel);
            await sut.ExecuteRun.Run(
                Arg.Any<ModPath>(), Arg.Any<(int Key, IPatcherRun Run)[]>(), Arg.Any<CancellationToken>(),
                Arg.Any<FilePath?>(), PersistenceMode.Text, Arg.Any<string?>());
        }
    }
}