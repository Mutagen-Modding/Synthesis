using FluentAssertions;
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
        ret.DataFolderPath.Should().Be(settings.DataFolderPath);
        ret.GameRelease.Should().Be(settings.GameRelease);
        ret.LoadOrderFilePath.Should().Be(settings.LoadOrderFilePath);
        ret.OutputPath.Should().Be(settings.OutputPath);
        ret.SourcePath.Should().Be(settings.SourcePath);
        ret.PatcherName.Should().Be(settings.PatcherName);
        ret.PersistencePath.Should().Be(settings.PersistencePath);
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
            .ExtraDataFolder.Should().Be(dir);
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
            .DefaultDataFolderPath.Should().BeNull();
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
            .DefaultDataFolderPath.Should().Be(existingDirectory);
    }
}