using System.Reactive;
using AutoFixture;
using FluentAssertions;
using NSubstitute;
using Synthesis.Bethesda.GUI.Settings;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.UI.Settings
{
    public class ShowHelpSettingsTests
    {
        [Theory, SynthAutoData]
        public void CommandDoesFlip(ShowHelpSetting sut)
        {
            var initial = sut.ShowHelp;
            sut.ShowHelpToggleCommand.CanExecute(Unit.Default)
                .Should().BeTrue();
            sut.ShowHelpToggleCommand.Execute(Unit.Default);
            sut.ShowHelp.Should().Be(!initial);
            sut.ShowHelpToggleCommand.Execute(Unit.Default);
            sut.ShowHelp.Should().Be(initial);
        }

        [Fact]
        public void CopiesInSettings()
        {
            var settings = Substitute.For<ISettingsSingleton>();
            settings.Gui.ShowHelp.Returns(true);
            var help = new ShowHelpSetting(
                new RetrieveSaveSettings(settings),
                settings);
            help.ShowHelp.Should().BeTrue();
            settings.Gui.ShowHelp.Returns(false);
            help = new ShowHelpSetting(
                new RetrieveSaveSettings(settings),
                settings);
            help.ShowHelp.Should().BeFalse();
        }

        [Theory, SynthAutoData]
        public void SavesSettings(ISettingsSingleton settings)
        {
            var retrieve = new RetrieveSaveSettings(settings);
            var help = new ShowHelpSetting(
                retrieve,
                settings);
            help.ShowHelp = true;
            retrieve.Retrieve(out var gui, out var pipe);
            gui.ShowHelp.Should().BeTrue();
            help.ShowHelp = false;
            retrieve.Retrieve(out gui, out pipe);
            gui.ShowHelp.Should().BeFalse();
        }
    }
}