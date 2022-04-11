using System.IO;
using FluentAssertions;
using Noggog;
using NSubstitute;
using Synthesis.Bethesda.Execution.Running.Cli;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.Running.Cli;

public class PipelineSettingsProviderTests
{
    [Theory, SynthAutoData]
    public void ProfileDefinitionPathMissingThrows(
        FilePath missingPath,
        PipelineProfilesProvider sut)
    {
        sut.ProfileDefinitionPathProvider.Path.Returns(missingPath);
        Assert.Throws<FileNotFoundException>(() =>
        {
            sut.Get();
        });
    }
        
    [Theory, SynthAutoData]
    public void PassesDefinitionPathToImporter(
        FilePath existingPath,
        PipelineProfilesProvider sut)
    {
        sut.ProfileDefinitionPathProvider.Path.Returns(existingPath);
        sut.Get();
        sut.PipelineSettingsImporter.Received(1).Import(existingPath);
    }
        
    [Theory, SynthAutoData]
    public void ReturnsImporterResults(
        FilePath existingPath,
        IPipelineSettings settings,
        PipelineProfilesProvider sut)
    {
        sut.ProfileDefinitionPathProvider.Path.Returns(existingPath);
        sut.PipelineSettingsImporter.Import(default!).ReturnsForAnyArgs(settings);
        sut.Get()
            .Should()
            .BeSameAs(settings.Profiles);
    }
}