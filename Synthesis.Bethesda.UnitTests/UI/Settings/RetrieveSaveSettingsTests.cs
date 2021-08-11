using Synthesis.Bethesda.GUI.Settings;
using Xunit;
using System;
using FluentAssertions;
using AutoFixture;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.Execution.Settings.V2;
using Synthesis.Bethesda.UnitTests.AutoData;

namespace Synthesis.Bethesda.UnitTests.UI.Settings
{
    public class RetrieveSaveSettingsTests
    {
        [Theory, SynthAutoData]
        public void Saves(RetrieveSaveSettings sut)
        {
            ISynthesisGuiSettings? gui = null;
            IPipelineSettings? pipe = null;
            sut.Saving.Subscribe(x =>
            {
                gui = x.Gui;
                pipe = x.Pipe;
                x.Gui.Should().NotBeNull();
                x.Pipe.Should().NotBeNull();
            });
            sut.Retrieve(out var guiRetrieved, out var pipeRetrieved);
            guiRetrieved.Should().Be(gui);
            pipeRetrieved.Should().Be(pipe);
        }
    }
}