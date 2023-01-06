using System.Diagnostics.CodeAnalysis;
using NuGet.Versioning;

namespace Synthesis.Bethesda.GUI.Services.Versioning;

public class SemanticVersionParsing
{
    public bool TryTrimAndParse(string str, [MaybeNullWhen(false)] out SemanticVersion semVer)
    {
        if (SemanticVersion.TryParse(str, out semVer)) return true;
        
        int GetNum(int i) => i == -1 ? 0 : i;
        if (Version.TryParse(str, out var version))
        {
            if (version.Revision != -1)
            {
                semVer = new SemanticVersion(GetNum(version.Major), GetNum(version.Minor), GetNum(version.Build), GetNum(version.Revision).ToString());
            }
            else
            {
                semVer = new SemanticVersion(GetNum(version.Major), GetNum(version.Minor), GetNum(version.Build));
            }
            
            return true;
        }

        semVer = default;
        return false;
    }
}