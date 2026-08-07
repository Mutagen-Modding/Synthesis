using Shouldly;
using Mutagen.Bethesda;
using Noggog;
using NSubstitute;
using Synthesis.Bethesda.Commands;
using Synthesis.Bethesda.Execution.Groups;
using Synthesis.Bethesda.Execution.Patchers.Running;
using Synthesis.Bethesda.Execution.Running.Runner;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.UnitTests.AutoData;

namespace Synthesis.Bethesda.UnitTests.Execution.Running.Runner;

public class RunArgsConstructorTests
{
    [Theory, SynthAutoData]
    public void PassesPatcherNameToSanitizer(
        IGroupRun groupRun,
        IPatcherPrepAndRun patcher,
        FilePath? sourcePath,
        RunParameters runParameters,
        RunArgsConstructor sut)
    {
        sut.GetArgs(groupRun, patcher, sourcePath, runParameters);
        sut.PatcherNameSanitizer.Received(1).Sanitize(patcher.Name);
    }
        
    [Theory, SynthAutoData]
    public void OutputPathUnderWorkingDirectory(
        IGroupRun groupRun,
        IPatcherPrepAndRun patcher,
        FilePath? sourcePath,
        RunParameters runParameters,
        RunArgsConstructor sut)
    {
        var result = sut.GetArgs(groupRun, patcher, sourcePath, runParameters);
        result.OutputPath.IsUnderneath(sut.ProfileDirectories.WorkingDirectory)
            .ShouldBeTrue();
    }
        
    [Theory, SynthAutoData]
    public void PatcherNameShouldBeSanitizedName(
        IGroupRun groupRun,
        IPatcherPrepAndRun patcher,
        FilePath? sourcePath,
        RunParameters runParameters,
        string sanitize,
        RunArgsConstructor sut)
    {
        sut.PatcherNameSanitizer.Sanitize(default!).ReturnsForAnyArgs(sanitize);
        var result = sut.GetArgs(groupRun, patcher, sourcePath, runParameters);
        result.PatcherName.ShouldBe(sanitize);
    }
        
    [Theory, SynthAutoData]
    public void OutputPathShouldNotContainOriginalName(
        IGroupRun groupRun,
        IPatcherPrepAndRun patcher,
        FilePath? sourcePath,
        RunParameters runParameters,
        string sanitize,
        RunArgsConstructor sut)
    {
        sut.PatcherNameSanitizer.Sanitize(default!).ReturnsForAnyArgs(sanitize);
        var result = sut.GetArgs(groupRun, patcher, sourcePath, runParameters);
        result.OutputPath.Name.String.ShouldNotContain(patcher.Name);
    }
        
    public static readonly HashSet<string> PipelineOnlyRunParameters = new()
    {
        nameof(RunParameters.PersistenceMode),
        nameof(RunParameters.Master),
        nameof(RunParameters.MasterStyle),
        nameof(RunParameters.MasterStyleFallbackEnabled),
        nameof(RunParameters.UpdateLoadOrderAfterRun),
    };

    [Theory, SynthAutoData]
    public void ForwardsEveryPatcherFacingRunParameter(
        IGroupRun groupRun,
        IPatcherPrepAndRun patcher,
        FilePath? sourcePath,
        RunParameters runParameters,
        RunArgsConstructor sut)
    {
        runParameters = runParameters with { PersistenceMode = PersistenceMode.Text };
        var result = sut.GetArgs(groupRun, patcher, sourcePath, runParameters);
        foreach (var source in typeof(RunParameters).GetProperties())
        {
            if (PipelineOnlyRunParameters.Contains(source.Name)) continue;
            var target = typeof(RunSynthesisPatcher).GetProperty(source.Name);
            target.ShouldNotBeNull($"{source.Name} has no patcher arg to forward to");
            $"{source.Name}: {target.GetValue(result)}"
                .ShouldBe($"{source.Name}: {source.GetValue(runParameters)}");
        }
    }

    [Theory, SynthAutoData]
    public void TypicalPassalong(
        IGroupRun groupRun,
        IPatcherPrepAndRun patcher,
        FilePath sourcePath,
        RunParameters runParameters,
        DirectoryPath dataDir,
        GameRelease release,
        FilePath loadOrderPath,
        RunArgsConstructor sut)
    {
        sut.DataDirectoryProvider.Path.Returns(dataDir);
        sut.ReleaseContext.Release.Returns(release);
        sut.RunLoadOrderPathProvider.PathFor(groupRun).Returns(loadOrderPath);
        var result = sut.GetArgs(groupRun, patcher, sourcePath, runParameters);
        result.SourcePath.ShouldBe(sourcePath);
        result.DataFolderPath.ShouldBe(dataDir);
        result.LoadOrderFilePath.ShouldBe(loadOrderPath);
    }
}