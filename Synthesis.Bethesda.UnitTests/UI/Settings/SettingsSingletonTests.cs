using System.IO;
using System.IO.Abstractions;
using FluentAssertions;
using NSubstitute;
using Synthesis.Bethesda.Execution;
using Synthesis.Bethesda.Execution.Pathing;
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
            var guiPaths = Substitute.For<IGuiSettingsPath>();
            var paths = Substitute.For<IPipelineSettingsPath>();
            guiPaths.Path.Returns("guisettings.json");
            paths.Path.Returns("pipesettings.json");
            fileSystem.File.Exists(guiPaths.Path).Returns(true);
            fileSystem.File.ReadAllTextAsync(guiPaths.Path).Returns(
                System.IO.File.ReadAllText("../../../../UI/Settings/guisettings.json"));
            fileSystem.File.Exists(paths.Path).Returns(true);
            fileSystem.File.ReadAllTextAsync(paths.Path).Returns(
                System.IO.File.ReadAllText("../../../../UI/Settings/pipelinesettings.json"));
            fileSystem.Directory.SetCurrentDirectory(Directory.GetCurrentDirectory());
            var settings = new SettingsSingleton(fileSystem, guiPaths, paths);
            settings.Gui.SelectedProfile.Should().Be("u2gktfpj.frs");
            settings.Pipeline.Profiles.Should().NotBeEmpty();
        }
    }
}