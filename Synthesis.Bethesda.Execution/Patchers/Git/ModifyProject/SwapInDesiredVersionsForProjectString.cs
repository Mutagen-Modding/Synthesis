using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Mutagen.Bethesda;
using Noggog;

namespace Synthesis.Bethesda.Execution.Patchers.Git.ModifyProject
{
    public interface ISwapInDesiredVersionsForProjectString
    {
        void Swap(
            XElement proj,
            string? mutagenVersion,
            out string? listedMutagenVersion,
            string? synthesisVersion,
            out string? listedSynthesisVersion,
            bool addMissing = true);
    }

    public class SwapInDesiredVersionsForProjectString : ISwapInDesiredVersionsForProjectString
    {
        internal static readonly HashSet<string> MutagenLibraries;

        static SwapInDesiredVersionsForProjectString()
        {
            MutagenLibraries = EnumExt.GetValues<GameCategory>()
                .Select(x => $"Mutagen.Bethesda.{x}")
                .And("Mutagen.Bethesda")
                .And("Mutagen.Bethesda.Core")
                .And("Mutagen.Bethesda.Kernel")
                .ToHashSet();
        }
        
        public void Swap(
            XElement proj,
            string? mutagenVersion,
            out string? listedMutagenVersion,
            string? synthesisVersion,
            out string? listedSynthesisVersion,
            bool addMissing = true)
        {
            listedMutagenVersion = null;
            listedSynthesisVersion = null;
            var missingLibs = new HashSet<string>(MutagenLibraries);
            XElement? itemGroup = null;
            foreach (var group in proj.Elements("ItemGroup"))
            {
                foreach (var elem in group.Elements().ToArray())
                {
                    if (!elem.Name.LocalName.Equals("PackageReference")) continue;
                    if (!elem.TryGetAttribute("Include", out var libAttr)) continue;
                    string swapInStr;
                    if (libAttr.Value.Equals("Mutagen.Bethesda.Synthesis"))
                    {
                        listedSynthesisVersion = elem.Attribute("Version")?.Value;
                        if (synthesisVersion == null) continue;
                        swapInStr = synthesisVersion;
                        missingLibs.Remove(libAttr.Value);
                    }
                    else if (MutagenLibraries.Contains(libAttr.Value))
                    {
                        listedMutagenVersion = elem.Attribute("Version")?.Value;
                        if (mutagenVersion == null) continue;
                        swapInStr = mutagenVersion;
                        missingLibs.Remove(libAttr.Value);
                    }
                    else
                    {
                        continue;
                    }
                    elem.SetAttributeValue("Version", swapInStr);
                }
                itemGroup = group;
            }
            if (itemGroup == null)
            {
                throw new ArgumentException("No ItemGroup found in project");
            }
            if (addMissing && mutagenVersion != null)
            {
                foreach (var missing in missingLibs)
                {
                    itemGroup.Add(new XElement("PackageReference",
                        new XAttribute("Include", missing),
                        new XAttribute("Version", mutagenVersion)));
                }
            }
        }
    }
}