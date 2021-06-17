using Synthesis.Bethesda.GUI.Settings;
using Xunit;
using System;
using FluentAssertions;
using AutoFixture;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.UnitTests.UI.Settings
{
    public class RetrieveSaveSettingsTests : IClassFixture<Fixture>
    {
        private readonly Fixture _Fixture;

        public RetrieveSaveSettingsTests(Fixture fixture)
        {
            _Fixture = fixture;
        }

        [Fact]
        public void Saves()
        {
            var settings = _Fixture.Inject.Create<ISettingsSingleton>();
            var retrieve = new RetrieveSaveSettings(settings);
            ISynthesisGuiSettings? gui = null;
            IPipelineSettings? pipe = null;
            retrieve.Saving.Subscribe(x =>
            {
                gui = x.Gui;
                pipe = x.Pipe;
                x.Gui.Should().NotBeNull();
                x.Pipe.Should().NotBeNull();
            });
            retrieve.Retrieve(out var guiRetrieved, out var pipeRetrieved);
            guiRetrieved.Should().Be(gui);
            pipeRetrieved.Should().Be(pipe);
        }
    }
}