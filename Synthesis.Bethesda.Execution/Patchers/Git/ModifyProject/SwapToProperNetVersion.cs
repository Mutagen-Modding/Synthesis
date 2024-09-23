using System.Xml.Linq;
using NuGet.Versioning;
using Serilog;
using Synthesis.Bethesda.Execution.Versioning;

namespace Synthesis.Bethesda.Execution.Patchers.Git.ModifyProject;

public interface ISwapToProperNetVersion
{
    void Swap(XElement proj, Version targetMutagenVersion);
}

public class SwapToProperNetVersion : ISwapToProperNetVersion
{
    private readonly ILogger _logger;
    private const int NetNum = 8;
    private readonly Version Net8Version = new Version(0, 45);

    public SwapToProperNetVersion(ILogger logger)
    {
        _logger = logger;
    }
    
    private void ProcessTargetFrameworkNode(XElement elem, Version targetMutagenVersion)
    {
        if (!elem.Name.LocalName.Equals("TargetFramework")) return;
        _logger.Information("Target mutagen version: {Target}", targetMutagenVersion);
        if (targetMutagenVersion < Net8Version)
        {
            ProcessLegacy(elem);
        }
        else
        {
            ProcessNet8(elem, targetMutagenVersion);
        }
    }

    private void ProcessLegacy(XElement elem)
    {
        _logger.Information("Processing as legacy mutagen version");
        if (elem.Value.Equals("netcoreapp3.1", StringComparison.Ordinal)
            || elem.Value.StartsWith("net5", StringComparison.Ordinal))
        {
            _logger.Information("Swapping to net6.0");
            elem.Value = "net6.0";
        }
    }

    private void ProcessNet8(XElement elem, Version targetMutagenVersion)
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
        
        _logger.Information("Swapping to net8.0");
        elem.Value = $"net{NetNum}.0";
    }

    public void Swap(XElement proj, Version targetMutagenVersion)
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