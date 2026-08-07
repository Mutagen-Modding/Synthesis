using Shouldly;
using Mutagen.Bethesda.Strings;
using Mutagen.Bethesda.Synthesis.CLI;
using Noggog;
using NSubstitute;
using Synthesis.Bethesda.Commands;
using Synthesis.Bethesda.Execution.Patchers.Running.Solution;
using Synthesis.Bethesda.UnitTests.AutoData;

namespace Synthesis.Bethesda.UnitTests.Execution.Patchers.Running.Solution;

public class ConstructSolutionPatcherRunArgsTests
{
    [Theory, SynthAutoData]
    public void BuildsOnTopOfBaseArgs(
        Language language,
        RunSynthesisPatcher settings,
        RunSynthesisMutagenPatcher baseArgs,
        ConstructSolutionPatcherRunArgs sut)
    {
        settings.TargetLanguage = language.ToString();
        sut.BaseRunArgs.Construct(settings).Returns(baseArgs);
        sut.Construct(settings).ShouldBeSameAs(baseArgs);
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
