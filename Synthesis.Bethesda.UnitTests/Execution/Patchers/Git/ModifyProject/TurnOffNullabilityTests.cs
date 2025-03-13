using System.Xml.Linq;
using Shouldly;
using Synthesis.Bethesda.Execution.Patchers.Git.Services.ModifyProject;
using Synthesis.Bethesda.UnitTests.AutoData;

namespace Synthesis.Bethesda.UnitTests.Execution.Patchers.Git.ModifyProject;

public class TurnOffNullabilityTests
{
    [Theory, SynthAutoData]
    public void RemoveWarning(
        TurnOffNullability sut)
    {
        var elem = new XElement("WarningsAsErrors", "nullable");
        var root = new XElement("Project",
            new XElement("PropertyGroup",
                elem));
        sut.TurnOff(root);
        elem.Value.ShouldBe(string.Empty);
    }

    [Theory, SynthAutoData]
    public void RemoveWarningFromMany(
        TurnOffNullability sut)
    {
        var elem = new XElement("WarningsAsErrors", "nullable,other");
        var root = new XElement("Project",
            new XElement("PropertyGroup",
                elem));
        sut.TurnOff(root);
        elem.Value.ShouldBe("other");
    }
}