using System.IO.Abstractions.TestingHelpers;
using AutoFixture;
using FluentAssertions;
using Serilog;
using Synthesis.Bethesda.GUI.Services;
using Synthesis.Bethesda.GUI.Services.Main;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.UI.Services;

public class IdeLocatorTests
{
    [Theory, SynthAutoData]
    public void MissingPathsDoesNotThrow(IdeLocator sut)
    {
        sut.VSPath.Should().BeNull();
        sut.RiderPath.Should().BeNull();
    }
}