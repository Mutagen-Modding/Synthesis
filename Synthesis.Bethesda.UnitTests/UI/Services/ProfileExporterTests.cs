using System.IO.Abstractions.TestingHelpers;
using AutoFixture.Xunit2;
using Shouldly;
using Noggog;
using NSubstitute;
using Synthesis.Bethesda.Execution.Pathing;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.Execution.Settings.V2;
using Synthesis.Bethesda.GUI.Services.Main;
using Synthesis.Bethesda.GUI.Services.Profile.Exporter;
using Synthesis.Bethesda.GUI.Settings;
using Synthesis.Bethesda.UnitTests.AutoData;

namespace Synthesis.Bethesda.UnitTests.UI.Services;

public class ProfileExporterTests
{
    private static (SynthesisGuiSettings Gui, PipelineSettings Pipe) Arrange(
        IRetrieveSaveSettings retrieve,
        IPipelineSettingsPath pipePaths,
        IGuiSettingsPath guiPaths,
        params ISynthesisProfileSettings[] profiles)
    {
        var gui = new SynthesisGuiSettings();
        var pipe = new PipelineSettings();
        foreach (var profile in profiles)
        {
            pipe.Profiles.Add(profile);
        }

        retrieve
            .When(x => x.Retrieve(out _, out _))
            .Do(ci =>
            {
                ci[0] = gui;
                ci[1] = pipe;
            });

        pipePaths.Path.Returns(new FilePath("/live/PipelineSettings.json"));
        guiPaths.Path.Returns(new FilePath("/live/GuiSettings.json"));
        return (gui, pipe);
    }

    [Theory, SynthAutoData]
    public void ExportsOnlySelectedProfile(
        [Frozen] IRetrieveSaveSettings retrieve,
        [Frozen] IPipelineSettingsPath pipePaths,
        [Frozen] IGuiSettingsPath guiPaths,
        MockFileSystem fs,
        ProfileExporter sut)
    {
        var (gui, pipe) = Arrange(retrieve, pipePaths, guiPaths,
            new SynthesisProfile { ID = "target" },
            new SynthesisProfile { ID = "other" });

        sut.Export("target");

        pipe.Profiles.ShouldHaveSingleItem().ID.ShouldBe("target");
        gui.SelectedProfile.ShouldBe("target");
    }

    [Theory, SynthAutoData]
    public void PinsVersioningAndDisablesAutoUpdate(
        [Frozen] IRetrieveSaveSettings retrieve,
        [Frozen] IPipelineSettingsPath pipePaths,
        [Frozen] IGuiSettingsPath guiPaths,
        MockFileSystem fs,
        ProfileExporter sut)
    {
        var gitPatcher = new GithubPatcherSettings
        {
            AutoUpdateToBranchTip = true,
            LatestTag = true,
        };
        var target = new SynthesisProfile
        {
            ID = "target",
            LockToCurrentVersioning = false,
            Groups = { new PatcherGroupSettings { Patchers = { gitPatcher } } },
        };
        Arrange(retrieve, pipePaths, guiPaths, target);

        sut.Export("target");

        target.LockToCurrentVersioning.ShouldBeTrue();
        gitPatcher.AutoUpdateToBranchTip.ShouldBeFalse();
        gitPatcher.LatestTag.ShouldBeFalse();
    }

    [Theory, SynthAutoData]
    public void WritesBothSettingsFilesInsideExportFolder(
        [Frozen] IRetrieveSaveSettings retrieve,
        [Frozen] IPipelineSettingsPath pipePaths,
        [Frozen] IGuiSettingsPath guiPaths,
        MockFileSystem fs,
        ProfileExporter sut)
    {
        Arrange(retrieve, pipePaths, guiPaths, new SynthesisProfile { ID = "target" });

        sut.Export("target");

        fs.File.Exists(fs.Path.Combine("Export", "PipelineSettings.json")).ShouldBeTrue();
        fs.File.Exists(fs.Path.Combine("Export", "GuiSettings.json")).ShouldBeTrue();
        fs.File.ReadAllText(fs.Path.Combine("Export", "PipelineSettings.json")).ShouldContain("target");
    }

    [Theory, SynthAutoData]
    public void DoesNotWriteToLiveSettingsPaths(
        [Frozen] IRetrieveSaveSettings retrieve,
        [Frozen] IPipelineSettingsPath pipePaths,
        [Frozen] IGuiSettingsPath guiPaths,
        MockFileSystem fs,
        ProfileExporter sut)
    {
        Arrange(retrieve, pipePaths, guiPaths, new SynthesisProfile { ID = "target" });

        sut.Export("target");

        // The settings path providers return rooted paths; the export must land inside
        // Export/ rather than clobbering the app's live settings at their real location.
        fs.File.Exists("/live/PipelineSettings.json").ShouldBeFalse();
        fs.File.Exists("/live/GuiSettings.json").ShouldBeFalse();
    }

    [Theory, SynthAutoData]
    public void WipesExistingExportFolderContents(
        [Frozen] IRetrieveSaveSettings retrieve,
        [Frozen] IPipelineSettingsPath pipePaths,
        [Frozen] IGuiSettingsPath guiPaths,
        MockFileSystem fs,
        ProfileExporter sut)
    {
        Arrange(retrieve, pipePaths, guiPaths, new SynthesisProfile { ID = "target" });
        fs.AddFile(fs.Path.Combine("Export", "stale.txt"), new MockFileData("old"));

        sut.Export("target");

        fs.File.Exists(fs.Path.Combine("Export", "stale.txt")).ShouldBeFalse();
        fs.File.Exists(fs.Path.Combine("Export", "PipelineSettings.json")).ShouldBeTrue();
    }

    [Theory, SynthAutoData]
    public void CopiesDataFolderWhenPresent(
        [Frozen] IRetrieveSaveSettings retrieve,
        [Frozen] IPipelineSettingsPath pipePaths,
        [Frozen] IGuiSettingsPath guiPaths,
        MockFileSystem fs,
        ProfileExporter sut)
    {
        Arrange(retrieve, pipePaths, guiPaths, new SynthesisProfile { ID = "target" });
        fs.AddFile(fs.Path.Combine("Data", "sub", "extra.txt"), new MockFileData("payload"));

        sut.Export("target");

        var copied = fs.Path.Combine("Export", "Data", "sub", "extra.txt");
        fs.File.Exists(copied).ShouldBeTrue();
        fs.File.ReadAllText(copied).ShouldBe("payload");
    }

    [Theory, SynthAutoData]
    public void DoesNotRequireDataFolder(
        [Frozen] IRetrieveSaveSettings retrieve,
        [Frozen] IPipelineSettingsPath pipePaths,
        [Frozen] IGuiSettingsPath guiPaths,
        MockFileSystem fs,
        ProfileExporter sut)
    {
        Arrange(retrieve, pipePaths, guiPaths, new SynthesisProfile { ID = "target" });

        sut.Export("target");

        fs.Directory.Exists(fs.Path.Combine("Export", "Data")).ShouldBeFalse();
    }

    [Theory, SynthAutoData]
    public void NavigatesToExportFolder(
        [Frozen] IRetrieveSaveSettings retrieve,
        [Frozen] IPipelineSettingsPath pipePaths,
        [Frozen] IGuiSettingsPath guiPaths,
        [Frozen] INavigateTo navigate,
        MockFileSystem fs,
        ProfileExporter sut)
    {
        Arrange(retrieve, pipePaths, guiPaths, new SynthesisProfile { ID = "target" });

        sut.Export("target");

        navigate.Received(1).Navigate(Arg.Is<DirectoryPath>(d => d.Name == "Export"));
    }

    [Theory, SynthAutoData]
    public void ThrowsWhenProfileNotFound(
        [Frozen] IRetrieveSaveSettings retrieve,
        [Frozen] IPipelineSettingsPath pipePaths,
        [Frozen] IGuiSettingsPath guiPaths,
        MockFileSystem fs,
        ProfileExporter sut)
    {
        Arrange(retrieve, pipePaths, guiPaths,
            new SynthesisProfile { ID = "other1" },
            new SynthesisProfile { ID = "other2" });

        Should.Throw<ArgumentException>(() => sut.Export("missing"));
    }
}
