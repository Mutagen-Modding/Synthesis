using System.Reactive;
using AutoFixture;
using FluentAssertions;
using NSubstitute;
using Synthesis.Bethesda.GUI.Settings;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.UI.Settings
{
    public class ShowHelpSettingsTests : IClassFixture<Fixture>
    {
        private readonly Fixture _Fixture;

        public ShowHelpSettingsTests(Fixture fixture)
        {
            _Fixture = fixture;
        }
        
        [Fact]
        public void CommandDoesFlip()
        {
            var help = new ShowHelpSetting(
                new RetrieveSaveSettings(),
                _Fixture.Inject.Create<ISettingsSingleton>());
            var initial = help.ShowHelp;
            help.ShowHelpToggleCommand.CanExecute(Unit.Default)
                .Should().BeTrue();
            help.ShowHelpToggleCommand.Execute(Unit.Default);
            help.ShowHelp.Should().Be(!initial);
            help.ShowHelpToggleCommand.Execute(Unit.Default);
            help.ShowHelp.Should().Be(initial);
        }

        [Fact]
        public void CopiesInSettings()
        {
            var settings = Substitute.For<ISettingsSingleton>();
            settings.Gui.ShowHelp.Returns(true);
            var help = new ShowHelpSetting(
                new RetrieveSaveSettings(),
                settings);
            help.ShowHelp.Should().BeTrue();
            settings.Gui.ShowHelp.Returns(false);
            help = new ShowHelpSetting(
                new RetrieveSaveSettings(),
                settings);
            help.ShowHelp.Should().BeFalse();
        }

        [Fact]
        public void SavesSettings()
        {
            var retrieve = new RetrieveSaveSettings();
            var help = new ShowHelpSetting(
                retrieve,
                _Fixture.Inject.Create<ISettingsSingleton>());
            help.ShowHelp = true;
            retrieve.Retrieve(out var gui, out var pipe);
            gui.ShowHelp.Should().BeTrue();
            help.ShowHelp = false;
            retrieve.Retrieve(out gui, out pipe);
            gui.ShowHelp.Should().BeFalse();
        }
    }
}