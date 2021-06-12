using Synthesis.Bethesda.GUI.Settings;
using Xunit;
using System;
using FluentAssertions;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.UnitTests.UI.Settings
{
    public class RetrieveSaveSettingsTests
    {
        [Fact]
        public void Saves()
        {
            var retrieve = new RetrieveSaveSettings();
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