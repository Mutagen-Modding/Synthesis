using Shouldly;
using Synthesis.Bethesda.GUI.Services.Main;
using Synthesis.Bethesda.UnitTests.AutoData;

namespace Synthesis.Bethesda.UnitTests.UI.Services;

public class IdeLocatorTests
{
    [Theory, SynthAutoData]
    public void MissingPathsDoesNotThrow(IdeLocator sut)
    {
        sut.VSPath.ShouldBeNull();
        sut.RiderPath.ShouldBeNull();
    }
}