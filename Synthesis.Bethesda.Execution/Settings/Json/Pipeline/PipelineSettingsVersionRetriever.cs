using System.IO;
using System.IO.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Noggog;

namespace Synthesis.Bethesda.Execution.Settings.Json.Pipeline;

public interface IPipelineSettingsVersionRetriever
{
    int GetVersion(FilePath path);
}

public class PipelineSettingsVersionRetriever : IPipelineSettingsVersionRetriever
{
    private readonly IFileSystem _fileSystem;

    public PipelineSettingsVersionRetriever(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }
        
    public int GetVersion(FilePath path)
    {
        using var reader = new StreamReader(_fileSystem.FileStream.Create(path, FileMode.Open, FileAccess.Read));
        JObject o = (JObject)JToken.ReadFrom(new JsonTextReader(reader));
        var versionNode = o["Version"];
        if (versionNode == null) return 1;
        return int.Parse(versionNode.ToString());
    }
}