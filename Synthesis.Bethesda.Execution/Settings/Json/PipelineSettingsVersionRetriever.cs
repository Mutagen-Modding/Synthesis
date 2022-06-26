using System.IO.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Noggog;

namespace Synthesis.Bethesda.Execution.Settings.Json;

public interface ISettingsVersionRetriever
{
    int? GetVersion(FilePath path);
}

public class SettingsVersionRetriever : ISettingsVersionRetriever
{
    private readonly IFileSystem _fileSystem;

    public SettingsVersionRetriever(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }
        
    public int? GetVersion(FilePath path)
    {
        using var reader = new StreamReader(_fileSystem.FileStream.Create(path, FileMode.Open, FileAccess.Read));
        JObject o = (JObject)JToken.ReadFrom(new JsonTextReader(reader));
        var versionNode = o["Version"];
        if (versionNode == null) return null;
        return int.Parse(versionNode.ToString());
    }
}