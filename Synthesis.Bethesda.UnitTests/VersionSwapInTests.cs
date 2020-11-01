using FluentAssertions;
using Noggog;
using Synthesis.Bethesda.Execution.Patchers.Git;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Xunit;

namespace Synthesis.Bethesda.UnitTests
{
    public class VersionSwapInTests
    {
        public void CreateProj(out XElement root, out XElement refs)
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

        public string CreateProj(params (string Library, string Version)[] nugets)
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

        [Fact]
        public void None()
        {
            var projStr = CreateProj();
            var projXml = XElement.Parse(projStr);
            GitPatcherRun.SwapInDesiredVersionsForProjectString(
                    projXml,
                    mutagenVersion: "0",
                    listedMutagenVersion: out var _,
                    synthesisVersion: "0",
                    listedSynthesisVersion: out var _,
                    addMissing: false);
            projXml.ToString()
                .Should()
                .BeEquivalentTo(projStr);
        }

        [Fact]
        public void NoUpgrade()
        {
            var projStr = CreateProj(
                ("Mutagen.Bethesda", "2.0"),
            ("Mutagen.Bethesda.Synthesis", "3.1"));
            var projXml = XElement.Parse(projStr);
            GitPatcherRun.SwapInDesiredVersionsForProjectString(
                projXml, 
                mutagenVersion: "2.0",
                listedMutagenVersion: out var _,
                synthesisVersion: "3.1",
                listedSynthesisVersion: out var _,
                addMissing: false);
            var swapString = projXml.ToString();
            var expectedString = CreateProj(
                ("Mutagen.Bethesda", "2.0"),
                ("Mutagen.Bethesda.Synthesis", "3.1"));
            swapString
                .Should()
                .BeEquivalentTo(expectedString);
        }

        [Fact]
        public void TypicalUpgrade()
        {
            var projStr = CreateProj(
                ("Mutagen.Bethesda", "0.0.0"),
                ("Mutagen.Bethesda.Synthesis", "0.1.0"));
            var projXml = XElement.Parse(projStr);
            GitPatcherRun.SwapInDesiredVersionsForProjectString(
                projXml,
                mutagenVersion: "2.0",
                listedMutagenVersion: out var _,
                synthesisVersion: "3.1",
                listedSynthesisVersion: out var _,
                addMissing: false);
            var swapString = projXml.ToString();
            var expectedString = CreateProj(
                ("Mutagen.Bethesda", "2.0"),
                ("Mutagen.Bethesda.Synthesis", "3.1"));
            swapString
                .Should()
                .BeEquivalentTo(expectedString);
        }

        [Fact]
        public void MultipleUpgrade()
        {
            var projStr = CreateProj(
                ("Mutagen.Bethesda", "0.0.0"),
                ("Mutagen.Bethesda.Oblivion", "0.1.0"),
                ("Mutagen.Bethesda.Synthesis", "0.1.0"));
            var projXml = XElement.Parse(projStr);
            GitPatcherRun.SwapInDesiredVersionsForProjectString(
                projXml,
                mutagenVersion: "2.0", 
                listedMutagenVersion: out var _,
                synthesisVersion: "3.1",
                listedSynthesisVersion: out var _,
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

        [Fact]
        public void AddMissingMutagen()
        {
            var projStr = CreateProj(
                ("Mutagen.Bethesda.Synthesis", "0.1.0"),
                ("Mutagen.Bethesda.Oblivion", "0.1.0"));
            var projXml = XElement.Parse(projStr);
            GitPatcherRun.SwapInDesiredVersionsForProjectString(
                projXml,
                mutagenVersion: "2.0",
                listedMutagenVersion: out var _,
                synthesisVersion: "3.1",
                listedSynthesisVersion: out var _,
                addMissing: true);
            var swapString = projXml.ToString();
            var expectedString = CreateProj(
                ("Mutagen.Bethesda.Synthesis", "3.1").AsEnumerable()
                    .And(GitPatcherRun.MutagenLibraries.Select(x => (x, "2.0")))
                    .ToArray());
            swapString
                .Should()
                .BeEquivalentTo(expectedString);
        }

        [Fact]
        public void RemoveWarning()
        {
            var elem = new XElement("WarningsAsErrors", "nullable");
            var root = new XElement("Project",
                new XElement("PropertyGroup",
                    elem));
            GitPatcherRun.TurnOffNullability(root);
            elem.Value.Should().BeEquivalentTo(string.Empty);
        }

        [Fact]
        public void RemoveWarningFromMany()
        {
            var elem = new XElement("WarningsAsErrors", "nullable,other");
            var root = new XElement("Project",
                new XElement("PropertyGroup",
                    elem));
            GitPatcherRun.TurnOffNullability(root);
            elem.Value.Should().BeEquivalentTo("other");
        }

        [Fact]
        public void Match()
        {
            var projStr = CreateProj(
                ("Mutagen.Bethesda", "0.0.0"),
                ("Mutagen.Bethesda.Oblivion", "0.1.0"),
                ("Mutagen.Bethesda.Synthesis", "0.3.0"));
            var projXml = XElement.Parse(projStr);
            GitPatcherRun.SwapInDesiredVersionsForProjectString(
                projXml,
                mutagenVersion: null,
                listedMutagenVersion: out var _,
                synthesisVersion: null,
                listedSynthesisVersion: out var _);
            var swapString = projXml.ToString();
            var expectedString = CreateProj(
                ("Mutagen.Bethesda", "0.0.0"),
                ("Mutagen.Bethesda.Oblivion", "0.1.0"),
                ("Mutagen.Bethesda.Synthesis", "0.3.0"));
            swapString
                .Should()
                .BeEquivalentTo(expectedString);
        }

        [Fact]
        public void NoMutagen()
        {
            var projStr = CreateProj(
                ("Mutagen.Bethesda.Synthesis", "0.3.0"));
            var projXml = XElement.Parse(projStr);
            GitPatcherRun.SwapInDesiredVersionsForProjectString(
                projXml,
                mutagenVersion: "0.1.0",
                listedMutagenVersion: out var _,
                synthesisVersion: null,
                listedSynthesisVersion: out var _);
            var swapString = projXml.ToString();
            var expectedString = CreateProj(
                ("Mutagen.Bethesda.Synthesis", "0.3.0"),
                ("Mutagen.Bethesda.Oblivion", "0.1.0"),
                ("Mutagen.Bethesda.Skyrim", "0.1.0"),
                ("Mutagen.Bethesda", "0.1.0"),
                ("Mutagen.Bethesda.Core", "0.1.0"),
                ("Mutagen.Bethesda.Kernel", "0.1.0"));
            swapString
                .Should()
                .BeEquivalentTo(expectedString);
        }
    }
}
