using System.Xml.Linq;
using Noggog;
using NuGet.Versioning;

namespace Synthesis.Bethesda.Execution.Patchers.Git.Services.ModifyProject;

public interface ISwapVersioning
{
    void Swap(XElement proj, string package, string version, SemanticVersion curVersion);
}

public class SwapVersioning : ISwapVersioning
{
    public void Swap(XElement proj, string package, string version, SemanticVersion curVersion)
    {
        foreach (var group in proj.Elements("ItemGroup"))
        {
            foreach (var elem in group.Elements().ToArray())
            {
                if (!elem.Name.LocalName.Equals("PackageReference")) continue;
                if (!elem.TryGetAttribute("Include", out var libAttr)) continue;
                if (!libAttr.Value.Equals(package, StringComparison.OrdinalIgnoreCase)) continue;
                if (!elem.TryGetAttribute("Version", out var existingVerStr)) continue;
                if (!SemanticVersion.TryParse(existingVerStr.Value, out var semVer))
                {
                    if (System.Version.TryParse(existingVerStr.Value, out var vers))
                    {
                        semVer = new SemanticVersion(
                            vers.Major,
                            vers.Minor,
                            vers.Build);
                    }
                    else
                    {
                        continue;
                    }
                }
                if (!semVer.Equals(curVersion)) continue;
                elem.SetAttributeValue("Version", version);
            }
        }
    }
}