using System.IO;
using System.IO.Abstractions;
using FluentAssertions;
using NSubstitute;
using Synthesis.Bethesda.GUI;
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
            fileSystem.File.Exists(Paths.GuiSettingsPath).Returns(true);
            fileSystem.File.ReadAllTextAsync(Paths.GuiSettingsPath).Returns(
                System.IO.File.ReadAllText("../../../../UI/Settings/guisettings.json"));
            fileSystem.File.Exists(Synthesis.Bethesda.Execution.Paths.SettingsFileName).Returns(true);
            fileSystem.File.ReadAllTextAsync(Synthesis.Bethesda.Execution.Paths.SettingsFileName).Returns(
                System.IO.File.ReadAllText("../../../../UI/Settings/pipelinesettings.json"));
            fileSystem.Directory.SetCurrentDirectory(Directory.GetCurrentDirectory());
            var settings = new SettingsSingleton(fileSystem);
            settings.Gui.SelectedProfile.Should().Be("u2gktfpj.frs");
            settings.Pipeline.Profiles.Should().NotBeEmpty();
        }
    }
}