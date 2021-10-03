using FluentAssertions;
using Noggog;
using NSubstitute;
using Synthesis.Bethesda.Commands;
using Synthesis.Bethesda.Execution.Patchers.Running.Solution;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.Patchers.Running.Solution
{
    public class ConstructSolutionPatcherRunArgsTests
    {
        [Theory, SynthAutoData]
        public void SettingsForwarding(
            RunSynthesisPatcher settings,
            ConstructSolutionPatcherRunArgs sut)
        {
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
            RunSynthesisPatcher settings,
            ConstructSolutionPatcherRunArgs sut)
        {
            sut.PatcherExtraDataPathProvider.Path.Returns(dir);
            sut.Construct(settings)
                .ExtraDataFolder.Should().Be(dir);
        }

        [Theory, SynthAutoData]
        public void DefaultDataFolderPathNullIfDoesNotExist(
            DirectoryPath missingDirectory,
            RunSynthesisPatcher settings,
            ConstructSolutionPatcherRunArgs sut)
        {
            sut.DefaultDataPathProvider.Path.Returns(missingDirectory);
            sut.Construct(settings)
                .DefaultDataFolderPath.Should().BeNull();
        }

        [Theory, SynthAutoData]
        public void DefaultDataFolderPathSetByProviderIfExists(
            DirectoryPath existingDirectory,
            RunSynthesisPatcher settings,
            ConstructSolutionPatcherRunArgs sut)
        {
            sut.DefaultDataPathProvider.Path.Returns(existingDirectory);
            sut.Construct(settings)
                .DefaultDataFolderPath.Should().Be(existingDirectory);
        }
    }
}