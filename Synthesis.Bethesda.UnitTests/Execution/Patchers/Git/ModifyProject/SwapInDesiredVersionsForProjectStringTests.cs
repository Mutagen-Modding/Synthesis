using System.Xml.Linq;
using FluentAssertions;
using Noggog;
using Synthesis.Bethesda.Execution.Patchers.Git.ModifyProject;
using Synthesis.Bethesda.Execution.Versioning;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.Patchers.Git.ModifyProject;

public class SwapInDesiredVersionsForProjectStringTests
{
    private static void CreateProj(out XElement root, out XElement refs)
    {
        refs = new XElement("ItemGroup");
        root = new XElement("Project",
            new XAttribute("Sdk", "Microsoft.NET.Sdk"),
            new XElement("PropertyGroup",
                new XElement("OutputType", "Exe"),
                new XElement("TargetFramework", "netcoreapp3.1"),
                new XElement("Nullable", "enable"),
                new XElement("WarningsAsErrors", "nullable")),
            refs);
    }

    private static string CreateProj(params (string Library, string Version)[] nugets)
    {
        CreateProj(out var root, out var refs);
        foreach (var nuget in nugets)
        {
            refs.Add(new XElement("PackageReference",
                new XAttribute("Include", nuget.Library),
                new XAttribute("Version", nuget.Version)));
        }
        return root.ToString();
    }
        
    [Theory, SynthAutoData]
    public void None(SwapInDesiredVersionsForProjectString sut)
    {
        var projStr = CreateProj();
        var projXml = XElement.Parse(projStr);
        sut.Swap(
            projXml,
            new NugetVersionPair(Mutagen: "0", Synthesis: "0"),
            out var _,
            addMissing: false);
        projXml.ToString()
            .Should()
            .BeEquivalentTo(projStr);
    }

    [Theory, SynthAutoData]
    public void NoUpgrade(SwapInDesiredVersionsForProjectString sut)
    {
        var projStr = CreateProj(
            ("Mutagen.Bethesda", "2.0"),
            ("Mutagen.Bethesda.Synthesis", "3.1"));
        var projXml = XElement.Parse(projStr);
        sut.Swap(
            projXml, 
            new NugetVersionPair(Mutagen: "2.0", Synthesis: "3.1"),
            out var _,
            addMissing: false);
        var swapString = projXml.ToString();
        var expectedString = CreateProj(
            ("Mutagen.Bethesda", "2.0"),
            ("Mutagen.Bethesda.Synthesis", "3.1"));
        swapString
            .Should()
            .BeEquivalentTo(expectedString);
    }

    [Theory, SynthAutoData]
    public void TypicalUpgrade(SwapInDesiredVersionsForProjectString sut)
    {
        var projStr = CreateProj(
            ("Mutagen.Bethesda", "0.0.0"),
            ("Mutagen.Bethesda.Synthesis", "0.1.0"));
        var projXml = XElement.Parse(projStr);
        sut.Swap(
            projXml,
            new NugetVersionPair(Mutagen: "2.0", Synthesis: "3.1"),
            out var _,
            addMissing: false);
        var swapString = projXml.ToString();
        var expectedString = CreateProj(
            ("Mutagen.Bethesda", "2.0"),
            ("Mutagen.Bethesda.Synthesis", "3.1"));
        swapString
            .Should()
            .BeEquivalentTo(expectedString);
    }

    [Theory, SynthAutoData]
    public void MultipleUpgrade(SwapInDesiredVersionsForProjectString sut)
    {
        var projStr = CreateProj(
            ("Mutagen.Bethesda", "0.0.0"),
            ("Mutagen.Bethesda.Oblivion", "0.1.0"),
            ("Mutagen.Bethesda.Synthesis", "0.1.0"));
        var projXml = XElement.Parse(projStr);
        sut.Swap(
            projXml,
            new NugetVersionPair(Mutagen: "2.0", Synthesis: "3.1"),
            out var _,
            addMissing: false);
        var swapString = projXml.ToString();
        var expectedString = CreateProj(
            ("Mutagen.Bethesda", "2.0"),
            ("Mutagen.Bethesda.Oblivion", "2.0"),
            ("Mutagen.Bethesda.Synthesis", "3.1"));
        swapString
            .Should()
            .BeEquivalentTo(expectedString);
    }

    [Theory, SynthAutoData]
    public void AddMissingMutagen(SwapInDesiredVersionsForProjectString sut)
    {
        var projStr = CreateProj(
            ("Mutagen.Bethesda.Synthesis", "0.1.0"),
            ("Mutagen.Bethesda.Oblivion", "0.1.0"));
        var projXml = XElement.Parse(projStr);
        sut.Swap(
            projXml,
            new NugetVersionPair(Mutagen: "2.0", Synthesis: "3.1"),
            out var _,
            addMissing: true);
        var swapString = projXml.ToString();
        var expectedString = CreateProj(
            ("Mutagen.Bethesda.Synthesis", "3.1").AsEnumerable()
            .And(SwapInDesiredVersionsForProjectString.MutagenLibraries.Select(x => (x, "2.0")))
            .ToArray());
        swapString
            .Should()
            .BeEquivalentTo(expectedString);
    }

    [Theory, SynthAutoData]
    public void Match(SwapInDesiredVersionsForProjectString sut)
    {
        var projStr = CreateProj(
            ("Mutagen.Bethesda", "0.0.0"),
            ("Mutagen.Bethesda.Oblivion", "0.1.0"),
            ("Mutagen.Bethesda.Synthesis", "0.3.0"));
        var projXml = XElement.Parse(projStr);
        sut.Swap(
            projXml,
            new NugetVersionPair(null, null),
            out var _);
        var swapString = projXml.ToString();
        var expectedString = CreateProj(
            ("Mutagen.Bethesda", "0.0.0"),
            ("Mutagen.Bethesda.Oblivion", "0.1.0"),
            ("Mutagen.Bethesda.Synthesis", "0.3.0"));
        swapString
            .Should()
            .BeEquivalentTo(expectedString);
    }

    [Theory, SynthAutoData]
    public void NoMutagen(SwapInDesiredVersionsForProjectString sut)
    {
        var projStr = CreateProj(
            ("Mutagen.Bethesda.Synthesis", "0.3.0"));
        var projXml = XElement.Parse(projStr);
        sut.Swap(
            projXml,
            new NugetVersionPair(Mutagen: "0.1.0", Synthesis: null),
            out var _);
        var swapString = projXml.ToString();
        var expectedString = CreateProj(
            ("Mutagen.Bethesda.Synthesis", "0.3.0"),
            ("Mutagen.Bethesda.Oblivion", "0.1.0"),
            ("Mutagen.Bethesda.Skyrim", "0.1.0"),
            ("Mutagen.Bethesda.Fallout4", "0.1.0"),
            ("Mutagen.Bethesda.Starfield", "0.1.0"),
            ("Mutagen.Bethesda", "0.1.0"),
            ("Mutagen.Bethesda.Core", "0.1.0"),
            ("Mutagen.Bethesda.Kernel", "0.1.0"));
        swapString
            .Should()
            .BeEquivalentTo(expectedString);
    }
}