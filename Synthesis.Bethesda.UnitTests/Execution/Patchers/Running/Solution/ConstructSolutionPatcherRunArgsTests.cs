using Shouldly;
using Mutagen.Bethesda.Strings;
using Noggog;
using NSubstitute;
using Synthesis.Bethesda.Commands;
using Synthesis.Bethesda.Execution.Patchers.Running.Solution;
using Synthesis.Bethesda.UnitTests.AutoData;

namespace Synthesis.Bethesda.UnitTests.Execution.Patchers.Running.Solution;

public class ConstructSolutionPatcherRunArgsTests
{
    [Theory, SynthAutoData]
    public void SettingsForwarding(
        Language language,
        RunSynthesisPatcher settings,
        ConstructSolutionPatcherRunArgs sut)
    {
        settings.TargetLanguage = language.ToString();
        var ret = sut.Construct(settings);
        ret.DataFolderPath.ShouldBe(settings.DataFolderPath);
        ret.GameRelease.ShouldBe(settings.GameRelease);
        ret.LoadOrderFilePath.ShouldBe(settings.LoadOrderFilePath);
        ret.OutputPath.ShouldBe(settings.OutputPath);
        ret.SourcePath.ShouldBe(settings.SourcePath);
        ret.PatcherName.ShouldBe(settings.PatcherName);
        ret.PersistencePath.ShouldBe(settings.PersistencePath);
    }

    [Theory, SynthAutoData]
    public void SetsExtraDataToProviderResult(
        DirectoryPath dir,
        Language language,
        RunSynthesisPatcher settings,
        ConstructSolutionPatcherRunArgs sut)
    {
        settings.TargetLanguage = language.ToString();
        sut.PatcherExtraDataPathProvider.Path.Returns(dir);
        sut.Construct(settings)
            .ExtraDataFolder.ShouldBe(dir);
    }

    [Theory, SynthAutoData]
    public void DefaultDataFolderPathNullIfDoesNotExist(
        DirectoryPath missingDirectory,
        Language language,
        RunSynthesisPatcher settings,
        ConstructSolutionPatcherRunArgs sut)
    {
        settings.TargetLanguage = language.ToString();
        sut.DefaultDataPathProvider.Path.Returns(missingDirectory);
        sut.Construct(settings)
            .DefaultDataFolderPath.ShouldBeNull();
    }

    [Theory, SynthAutoData]
    public void DefaultDataFolderPathSetByProviderIfExists(
        DirectoryPath existingDirectory,
        Language language,
        RunSynthesisPatcher settings,
        ConstructSolutionPatcherRunArgs sut)
    {
        settings.TargetLanguage = language.ToString();
        sut.DefaultDataPathProvider.Path.Returns(existingDirectory);
        sut.Construct(settings)
            .DefaultDataFolderPath.ShouldBe(existingDirectory);
    }
}