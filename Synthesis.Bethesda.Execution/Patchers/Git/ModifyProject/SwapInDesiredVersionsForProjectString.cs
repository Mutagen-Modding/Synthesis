using System.Xml.Linq;
using Mutagen.Bethesda;
using Noggog;
using Synthesis.Bethesda.Execution.Versioning;

namespace Synthesis.Bethesda.Execution.Patchers.Git.ModifyProject;

public interface ISwapInDesiredVersionsForProjectString
{
    void Swap(
        XElement proj,
        NugetVersionPair versions,
        out NugetVersionPair listedVersions,
        bool addMissing = true);
}

public class SwapInDesiredVersionsForProjectString : ISwapInDesiredVersionsForProjectString
{
    internal static readonly HashSet<string> MutagenLibraries;

    static SwapInDesiredVersionsForProjectString()
    {
        MutagenLibraries = Enums<GameCategory>.Values
            .Select(x => $"Mutagen.Bethesda.{x}")
            .And("Mutagen.Bethesda")
            .And("Mutagen.Bethesda.Core")
            .And("Mutagen.Bethesda.Kernel")
            .ToHashSet();
    }
        
    public void Swap(
        XElement proj,
        NugetVersionPair versions,
        out NugetVersionPair listedVersions,
        bool addMissing = true)
    {
        listedVersions = new NugetVersionPair(null, null);
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
                    listedVersions = listedVersions with
                    {
                        Synthesis = elem.Attribute("Version")?.Value
                    };
                    if (versions.Synthesis == null) continue;
                    swapInStr = versions.Synthesis;
                    missingLibs.Remove(libAttr.Value);
                }
                else if (MutagenLibraries.Contains(libAttr.Value))
                {
                    listedVersions = listedVersions with
                    {
                        Mutagen = elem.Attribute("Version")?.Value
                    };
                    if (versions.Mutagen == null) continue;
                    swapInStr = versions.Mutagen;
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
        if (addMissing && versions.Mutagen != null)
        {
            foreach (var missing in missingLibs)
            {
                itemGroup.Add(new XElement("PackageReference",
                    new XAttribute("Include", missing),
                    new XAttribute("Version", versions.Mutagen)));
            }
        }
    }
}