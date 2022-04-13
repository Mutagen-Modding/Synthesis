using System.IO;
using System.IO.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Noggog;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.Execution.Settings.Json;

namespace Synthesis.Bethesda.GUI.Json;

public class PipelineSettingsMigration
{
    private readonly IFileSystem _fileSystem;
    private readonly ISettingsVersionRetriever _versionRetriever;

    public PipelineSettingsMigration(
        IFileSystem fileSystem,
        ISettingsVersionRetriever versionRetriever)
    {
        _fileSystem = fileSystem;
        _versionRetriever = versionRetriever;
    }
    
    public void Upgrade(IPipelineSettings settings, FilePath guiPath)
    {
        var vers = _versionRetriever.GetVersion(guiPath);
        if (vers >= 2) return;
        using var reader = new StreamReader(_fileSystem.FileStream.Create(guiPath, FileMode.Open, FileAccess.Read));
        JObject o = (JObject)JToken.ReadFrom(new JsonTextReader(reader));
        var node = o["WorkingDirectory"];
        if (node != null)
        {
            settings.WorkingDirectory = node.ToString();
        }
        node = o["BuildCorePercentage"];
        if (node != null
            && double.TryParse(node.ToString(), out var buildPerc))
        {
            settings.BuildCorePercentage = buildPerc;
        }
        node = o["DotNetPathOverride"];
        if (node != null)
        {
            settings.DotNetPathOverride = node.ToString();
        }
    }
}