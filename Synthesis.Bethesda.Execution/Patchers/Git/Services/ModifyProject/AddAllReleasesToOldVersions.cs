using System.Xml.Linq;
using Mutagen.Bethesda;
using Noggog;
using NuGet.Versioning;

namespace Synthesis.Bethesda.Execution.Patchers.Git.Services.ModifyProject;

public class AddAllReleasesToOldVersions
{
    public static readonly SemanticVersion IntroducingSynthesisVersion = new(0, 29, 0);
        
    public void Add(
        XElement proj, 
        SemanticVersion? curSynthVersion, 
        SemanticVersion targetMutagenVersion, 
        SemanticVersion? targetSynthVersion)
    {
        if (targetSynthVersion != null
            && curSynthVersion != null
            && curSynthVersion < IntroducingSynthesisVersion
            && targetSynthVersion >= IntroducingSynthesisVersion)
        {
            foreach (var group in proj.Elements("ItemGroup"))
            {
                foreach (var elem in group.Elements())
                {
                    if (!elem.Name.LocalName.Equals("PackageReference")) continue;
                    if (!elem.TryGetAttribute("Include", out var include)) continue;
                    if (include.Value.Equals("Mutagen.Bethesda")) return;
                    foreach (var cat in Enums<GameCategory>.Values)
                    {
                        if (include.Value.Equals($"Mutagen.Bethesda.{cat}")) return;
                    }
                }
            }

            proj.Add(new XElement("ItemGroup",
                new XElement("PackageReference",
                    new XAttribute("Include", "Mutagen.Bethesda"),
                    new XAttribute("Version", targetMutagenVersion))));
        }
    }
}