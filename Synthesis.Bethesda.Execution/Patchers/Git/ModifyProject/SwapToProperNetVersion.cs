using System.Xml.Linq;
using NuGet.Versioning;
using Synthesis.Bethesda.Execution.Versioning;

namespace Synthesis.Bethesda.Execution.Patchers.Git.ModifyProject;

public interface ISwapToProperNetVersion
{
    void Swap(XElement proj, NuGetVersion targetMutagenVersion);
}

public class SwapToProperNetVersion : ISwapToProperNetVersion
{
    private const int NetNum = 8;
    private readonly NuGetVersion Net8Version = new NuGetVersion(0, 45, 0);
    
    private void ProcessTargetFrameworkNode(XElement elem, NuGetVersion targetMutagenVersion)
    {
        if (!elem.Name.LocalName.Equals("TargetFramework")) return;
        if (targetMutagenVersion < Net8Version)
        {
            ProcessLegacy(elem);
        }
        else
        {
            ProcessNet8(elem, targetMutagenVersion);
        }
    }

    private static void ProcessLegacy(XElement elem)
    {
        if (elem.Value.Equals("netcoreapp3.1", StringComparison.Ordinal)
            || elem.Value.StartsWith("net5", StringComparison.Ordinal))
        {
            elem.Value = "net6.0";
        }
    }

    private static void ProcessNet8(XElement elem, NuGetVersion targetMutagenVersion)
    {
        if (!elem.Value.StartsWith("net"))
        {
            throw new ArgumentException($"Could not process version: {elem.Value}");
        }

        if (double.TryParse(elem.Value.Substring(3), out var netNum)
            && netNum > NetNum)
        {
            return;
        }
        
        elem.Value = $"net{NetNum}.0";
    }

    public void Swap(XElement proj, NuGetVersion targetMutagenVersion)
    {
        foreach (var group in proj.Elements("PropertyGroup"))
        {
            foreach (var elem in group.Elements())
            {
                ProcessTargetFrameworkNode(elem, targetMutagenVersion);
            }
        }
    }
}