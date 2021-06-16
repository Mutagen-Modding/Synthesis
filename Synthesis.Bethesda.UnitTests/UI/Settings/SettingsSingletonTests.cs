using System.IO;
using System.IO.Abstractions;
using FluentAssertions;
using NSubstitute;
using Synthesis.Bethesda.Execution;
using Synthesis.Bethesda.GUI.Services;
using Synthesis.Bethesda.GUI.Settings;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.UI.Settings
{
    public class SettingsSingletonTests
    {
        [Fact]
        public void CanImport()
        {
            var fileSystem = Substitute.For<IFileSystem>();
            var guiPaths = Substitute.For<IGuiPaths>();
            var paths = Substitute.For<IPaths>();
            guiPaths.GuiSettingsPath.Returns("guisettings.json");
            paths.SettingsFileName.Returns("pipesettings.json");
            fileSystem.File.Exists(guiPaths.GuiSettingsPath).Returns(true);
            fileSystem.File.ReadAllTextAsync(guiPaths.GuiSettingsPath).Returns(
                System.IO.File.ReadAllText("../../../../UI/Settings/guisettings.json"));
            fileSystem.File.Exists(paths.SettingsFileName).Returns(true);
            fileSystem.File.ReadAllTextAsync(paths.SettingsFileName).Returns(
                System.IO.File.ReadAllText("../../../../UI/Settings/pipelinesettings.json"));
            fileSystem.Directory.SetCurrentDirectory(Directory.GetCurrentDirectory());
            var settings = new SettingsSingleton(fileSystem, guiPaths, paths);
            settings.Gui.SelectedProfile.Should().Be("u2gktfpj.frs");
            settings.Pipeline.Profiles.Should().NotBeEmpty();
        }
    }
}