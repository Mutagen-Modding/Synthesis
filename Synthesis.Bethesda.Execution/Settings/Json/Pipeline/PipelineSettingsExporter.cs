using System.IO.Abstractions;
using Newtonsoft.Json;
using Noggog;

namespace Synthesis.Bethesda.Execution.Settings.Json.Pipeline;

public interface IPipelineSettingsExporter
{
    void Write(FilePath path, IPipelineSettings pipe);
}

public class PipelineSettingsExporter : IPipelineSettingsExporter
{
    private readonly IFileSystem _fileSystem;

    public PipelineSettingsExporter(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }
        
    public void Write(FilePath path, IPipelineSettings pipe)
    {
        _fileSystem.File.WriteAllText(path,
            JsonConvert.SerializeObject(pipe, Formatting.Indented, Execution.Constants.JsonSettings));
    }
}