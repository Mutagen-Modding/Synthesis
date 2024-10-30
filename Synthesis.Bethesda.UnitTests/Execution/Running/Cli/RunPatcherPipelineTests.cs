using Noggog;
using NSubstitute;
using Synthesis.Bethesda.CLI.RunPipeline;
using Synthesis.Bethesda.Execution.Groups;
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
            Arg.Any<RunParameters>());
    }
        
    [Theory, SynthAutoData]
    public async Task PassesTypicalSettings(
        CancellationToken cancel,
        RunPatcherPipeline sut)
    {
        sut.Command.PersistenceMode = PersistenceMode.None;
        await sut.Run(cancel);
        await sut.ExecuteRun.Received(1).Run(
            Arg.Any<IGroupRun[]>(),
            Arg.Any<CancellationToken>(),
            outputDir: sut.Command.OutputDirectory,
            runParameters: new RunParameters(
                TargetLanguage: sut.ProfileSettings.TargetLanguage,
                Localize: sut.ProfileSettings.Localize,
                UseUtf8ForEmbeddedStrings: sut.ProfileSettings.UseUtf8ForEmbeddedStrings,
                HeaderVersionOverride: sut.ProfileSettings.HeaderVersionOverride,
                FormIDRangeMode: sut.ProfileSettings.FormIDRangeMode,
                PersistenceMode: sut.Command.PersistenceMode.Value,
                PersistencePath: sut.Command.PersistencePath,
                Master: sut.ProfileSettings.ExportAsMasterFiles,
                MasterStyle: sut.ProfileSettings.MasterStyle,
                MasterStyleFallbackEnabled: sut.ProfileSettings.MasterStyleFallbackEnabled));
    }
        
    [Theory, SynthAutoData]
    public async Task NullPersistenceModeFallsBackToNone(
        CancellationToken cancel,
        RunPatcherPipeline sut)
    {
        sut.Command.PersistenceMode = null;
        await sut.Run(cancel);
        await sut.ExecuteRun.Received(1).Run(
            Arg.Any<IGroupRun[]>(), Arg.Any<CancellationToken>(), Arg.Any<DirectoryPath>(),
            Arg.Any<RunParameters>());
    }
}