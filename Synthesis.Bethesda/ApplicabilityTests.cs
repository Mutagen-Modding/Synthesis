using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Synthesis.Bethesda;

public static class ApplicabilityTests
{
    public static bool IsMutagenPatcherProject(string projPath)
    {
        var projXml = XElement.Parse(File.ReadAllText(projPath));
        foreach (var group in projXml.Elements("ItemGroup"))
        {
            foreach (var elem in group.Elements().ToArray())
            {
                if (!elem.Name.LocalName.Equals("PackageReference")) continue;
                var attr = elem.Attribute("Include");
                if ("Mutagen.Bethesda.Synthesis".Equals(attr?.Value)) return true;
            }
        }
        return false;
    }
}