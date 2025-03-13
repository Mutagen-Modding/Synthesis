using System.Globalization;
using System.Xml.Linq;
using Serilog;
using Noggog;
using NuGet.Versioning;

namespace Synthesis.Bethesda.Execution.Patchers.Git.Services.ModifyProject;

public interface ISwapToProperNetVersion
{
    void Swap(XElement proj, SemanticVersion targetMutagenVersion);
}

public class SwapToProperNetVersion : ISwapToProperNetVersion
{
    private readonly ILogger _logger;
    private readonly SemanticVersion Net8Version = new(0, 45, 0);
    // private readonly Version Net9Version = new Version(0, 49);
    private readonly CultureInfo Culture = new("en");

    private readonly SortedList<SemanticVersion, byte> _netMapping;
    
    public SwapToProperNetVersion(ILogger logger)
    {
        _logger = logger;
        _netMapping = new SortedList<SemanticVersion, byte>()
        {
            { Net8Version, 8 },
            // { Net9Version, 9 }
        };
    }
    
    private void ProcessTargetFrameworkNode(XElement elem, SemanticVersion targetMutagenVersion)
    {
        if (!elem.Name.LocalName.Equals("TargetFramework")) return;
        _logger.Information("Target mutagen version: {Target}", targetMutagenVersion);
        if (targetMutagenVersion < Net8Version)
        {
            ProcessLegacy(elem);
        }
        else
        {
            ProcessNormal(elem, targetMutagenVersion);
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

    private bool NeedsUpgrade(string elem, byte target)
    {
        if (elem.StartsWith("netcoreapp", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        if (!elem.StartsWith("net", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Could not process version: {elem}");
        }

        if (double.TryParse(elem.Substring(3), Culture, out var netNum)
            && netNum > target)
        {
            _logger.Information($"Already net{netNum}.  No need to upgrade");
            return false;
        }

        return true;
    }

    private void ProcessNormal(XElement elem, SemanticVersion targetMutagenVersion)
    {
        if (!_netMapping.TryGetInDirection(targetMutagenVersion, higher: false, out var targetNetVersion))
        {
            throw new ArgumentException($"Could not find target net to use for version: {targetMutagenVersion}");
        }

        if (!NeedsUpgrade(elem.Value, targetNetVersion.Value)) return;

        elem.Value = $"net{targetNetVersion.Value}.0";
        _logger.Information("Swapping to {Target}", elem.Value);
    }

    public void Swap(XElement proj, SemanticVersion targetMutagenVersion)
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