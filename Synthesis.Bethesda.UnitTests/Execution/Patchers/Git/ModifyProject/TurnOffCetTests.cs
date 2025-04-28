using System.Xml.Linq;
using Shouldly;
using Synthesis.Bethesda.Execution.Patchers.Git.Services.ModifyProject;
using Synthesis.Bethesda.UnitTests.AutoData;

namespace Synthesis.Bethesda.UnitTests.Execution.Patchers.Git.ModifyProject;

public class TurnOffCetTests
{
    [Theory, SynthAutoData]
    public void AdjustCetCompat(
        TurnOffCetCompat sut)
    {
        var elem = new XElement("CETCompat", "true");
        var root = new XElement("Project",
            new XElement("PropertyGroup",
                elem));
        sut.TurnOff(root);
        elem.Value.ShouldBe("false");
    }

    [Theory, SynthAutoData]
    public void AddCetCompat(
        TurnOffCetCompat sut)
    {
        var root = new XElement("Project",
            new XElement("PropertyGroup"));
        sut.TurnOff(root);
        root.Element("PropertyGroup")!
            .Element("CETCompat")!
            .Value.ShouldBe("false");
    }
}