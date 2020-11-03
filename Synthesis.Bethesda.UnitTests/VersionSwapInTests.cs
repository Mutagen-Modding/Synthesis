using FluentAssertions;
using Noggog;
using Synthesis.Bethesda.Execution;
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
                    new XElement("TargetFramework", "netcoreapp3.1")),
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
            GitPatcherRun.SwapInDesiredVersionsForProjectString(
                    projStr, 
                    mutagenVersion: "0",
                    listedMutagenVersion: out var _,
                    synthesisVersion: "0",
                    listedSynthesisVersion: out var _,
                    addMissing: false)
                .Should()
                .BeEquivalentTo(projStr);
        }

        [Fact]
        public void NoUpgrade()
        {
            var projStr = CreateProj(
                ("Mutagen.Bethesda", "2.0"),
            ("Mutagen.Bethesda.Synthesis", "3.1"));
            var swapString = GitPatcherRun.SwapInDesiredVersionsForProjectString(
                projStr, 
                mutagenVersion: "2.0",
                listedMutagenVersion: out var _,
                synthesisVersion: "3.1",
                listedSynthesisVersion: out var _,
                addMissing: false);
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
            var swapString = GitPatcherRun.SwapInDesiredVersionsForProjectString(
                projStr,
                mutagenVersion: "2.0",
                listedMutagenVersion: out var _,
                synthesisVersion: "3.1",
                listedSynthesisVersion: out var _,
                addMissing: false);
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
            var swapString = GitPatcherRun.SwapInDesiredVersionsForProjectString(
                projStr,
                mutagenVersion: "2.0",
                listedMutagenVersion: out var _,
                synthesisVersion: "3.1",
                listedSynthesisVersion: out var _,
                addMissing: false);
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
            var swapString = GitPatcherRun.SwapInDesiredVersionsForProjectString(
                projStr,
                mutagenVersion: "2.0",
                listedMutagenVersion: out var _,
                synthesisVersion: "3.1",
                listedSynthesisVersion: out var _,
                addMissing: true);
            var expectedString = CreateProj(
                ("Mutagen.Bethesda.Synthesis", "3.1").AsEnumerable()
                    .And(GitPatcherRun.MutagenLibraries.Select(x => (x, "2.0")))
                    .ToArray());
            swapString
                .Should()
                .BeEquivalentTo(expectedString);
        }
    }
}
