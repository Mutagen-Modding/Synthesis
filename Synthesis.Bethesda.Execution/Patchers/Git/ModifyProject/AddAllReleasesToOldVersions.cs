using System.Xml.Linq;
using Noggog;
using NuGet.Versioning;

namespace Synthesis.Bethesda.Execution.Patchers.Git.ModifyProject;

public class AddAllReleasesToOldVersions
{
    public static readonly Version IntroducingSynthesisVersion = new Version(0, 29);
        
    public void Add(
        XElement proj, 
        Version? curSynthVersion, 
        NuGetVersion targetMutagenVersion, 
        NuGetVersion? targetSynthVersion)
    {
        if (targetSynthVersion != null
            && curSynthVersion != null
            && curSynthVersion < IntroducingSynthesisVersion
            && targetSynthVersion.Version >= IntroducingSynthesisVersion)
        {
            foreach (var group in proj.Elements("ItemGroup"))
            {
                foreach (var elem in group.Elements())
                {
                    if (!elem.Name.LocalName.Equals("PackageReference")) continue;
                    if (!elem.TryGetAttribute("Include", out var include)) continue;
                    if (include.Value.Equals("Mutagen.Bethesda")) return;
                }
            }

            proj.Add(new XElement("ItemGroup",
                new XElement("PackageReference",
                    new XAttribute("Include", "Mutagen.Bethesda"),
                    new XAttribute("Version", targetMutagenVersion))));
        }
    }
}