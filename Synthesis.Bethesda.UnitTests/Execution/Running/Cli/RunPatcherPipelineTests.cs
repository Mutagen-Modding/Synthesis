using System;
using System.Threading;
using System.Threading.Tasks;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Strings;
using Noggog;
using NSubstitute;
using Synthesis.Bethesda.CLI.RunPipeline;
using Synthesis.Bethesda.Execution.Groups;
using Synthesis.Bethesda.Execution.Patchers.Running;
using Synthesis.Bethesda.Execution.Running.Runner;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.Running.Cli;

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
            Arg.Any<RunParameters>(), Arg.Any<FilePath?>());
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
            runParameters: new RunParameters(
                sut.ProfileSettings.TargetLanguage,
                sut.ProfileSettings.Localize,
                sut.Instructions.PersistenceMode.Value,
                sut.Instructions.PersistencePath),
            sourcePath: sut.Instructions.SourcePath);
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
            Arg.Any<RunParameters>(), Arg.Any<FilePath?>());
    }
}