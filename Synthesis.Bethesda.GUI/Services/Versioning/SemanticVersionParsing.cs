using System.Diagnostics.CodeAnalysis;
using Noggog;
using NuGet.Versioning;

namespace Synthesis.Bethesda.GUI.Services.Versioning;

public class SemanticVersionParsing
{
    public bool TryTrimAndParse(string str, [MaybeNullWhen(false)] out SemanticVersion semVer)
    {
        if (str.EndsWith("--1"))
        {
            str = str.TrimEnd("--1");
        }

        str = TrimZero(str);
        
        if (SemanticVersion.TryParse(str, out semVer)) return true;
        
        int GetNum(int i) => i == -1 ? 0 : i;
        if (Version.TryParse(str, out var version))
        {
            semVer = new SemanticVersion(GetNum(version.Major), GetNum(version.Minor), GetNum(version.Build), version.Revision.ToString());
            return true;
        }

        semVer = default;
        return false;
    }

    private string TrimZero(string str)
    {
        while (str.EndsWith(".0"))
        {
            str = str.TrimEnd(".0");
        }

        return str;
    }
}