using AutoFixture.Xunit2;
using FluentAssertions;
using Noggog;
using NSubstitute;
using Synthesis.Bethesda.Execution.Pathing;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.Execution.Settings.Json.Pipeline;
using Synthesis.Bethesda.GUI.Json;
using Synthesis.Bethesda.GUI.Services.Main;
using Synthesis.Bethesda.GUI.Settings;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.UI.Settings;

public class SettingsSingletonTests
{
    // [Theory, SynthAutoData]
    // public void CanImport(
    //     SettingsSingleton sut)
    // {
    //     sut.GuiPaths.Path.Returns(new FilePath("guisettings.json"));
    //     sut.Paths.Path.Returns(new FilePath("pipesettings.json"));
    //     sut.FileSystem.File.Exists(sut.GuiPaths.Path).Returns(true);
    //     sut.FileSystem.File.ReadAllTextAsync(sut.GuiPaths.Path).Returns(
    //         System.IO.File.ReadAllText("../../../../UI/Settings/guisettings.json"));
    //     sut.FileSystem.File.Exists(sut.Paths.Path).Returns(true);
    //     sut.FileSystem.File.ReadAllTextAsync(sut.Paths.Path).Returns(
    //         System.IO.File.ReadAllText("../../../../UI/Settings/pipelinesettings.json"));
    //     sut.FileSystem.Directory.SetCurrentDirectory(Directory.GetCurrentDirectory());
    //     
    //     sut.Gui.SelectedProfile.Should().Be("u2gktfpj.frs");
    //     sut.Pipeline.Profiles.Should().NotBeEmpty();
    // }

    [Theory, SynthAutoData(GenerateDelegates: true)]
    public void ReturnsDefaultGuiIfMissing(
        [Frozen]IGuiSettingsPath guiPaths,
        [Frozen]IGuiSettingsImporter settingsImporter,
        FilePath missingPath,
        ISynthesisGuiSettings settings,
        Func<SettingsSingleton> sut)
    {
        settingsImporter.Import(default).ReturnsForAnyArgs(settings);
        guiPaths.Path.Returns(missingPath);
        sut().Gui.Should().Be(new SynthesisGuiSettings());
    }

    [Theory, SynthAutoData(GenerateDelegates: true)]
    public void ReturnsImportedGuiIfFileExists(
        FilePath existingPath,
        [Frozen]IGuiSettingsPath guiPaths,
        [Frozen]IGuiSettingsImporter settingsImporter,
        ISynthesisGuiSettings settings,
        Func<SettingsSingleton> sut)
    {
        settingsImporter.Import(existingPath).Returns(settings);
        guiPaths.Path.Returns(existingPath);
        sut().Gui.Should().Be(settings);
    }
        
    [Theory, SynthAutoData(GenerateDelegates: true)]
    public void ReturnsDefaultPipeIfMissing(
        [Frozen]IPipelineSettingsPath paths,
        [Frozen]IPipelineSettingsImporter settingsImporter,
        IPipelineSettings settings,
        FilePath missingPath,
        Func<SettingsSingleton> sut)
    {
        settingsImporter.Import(default).ReturnsForAnyArgs(settings);
        paths.Path.Returns(missingPath);
        sut().Pipeline.Profiles.Should().BeEmpty();
    }

    [Theory, SynthAutoData(GenerateDelegates: true)]
    public void ReturnsImportedPipeIfFileExists(
        FilePath existingPath,
        [Frozen]IPipelineSettingsPath paths,
        [Frozen]IPipelineSettingsImporter settingsImporter,
        IPipelineSettings settings,
        Func<SettingsSingleton> sut)
    {
        settingsImporter.Import(existingPath).Returns(settings);
        paths.Path.Returns(existingPath);
        sut().Pipeline.Should().Be(settings);
    }
}