using System.IO.Abstractions;
using Newtonsoft.Json;
using Noggog;
using Synthesis.Bethesda.Execution.Settings.V2;

namespace Synthesis.Bethesda.Execution.Settings.Json.Pipeline.V2;

public interface IPipelineSettingsV2Reader
{
    PipelineSettings Read(FilePath path);
}

public class PipelineSettingsV2Reader : IPipelineSettingsV2Reader
{
    private readonly IFileSystem _fileSystem;

    public PipelineSettingsV2Reader(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }
        
    public PipelineSettings Read(FilePath path)
    {
        return JsonConvert.DeserializeObject<PipelineSettings>(
            _fileSystem.File.ReadAllText(path),
            Constants.JsonSettings)!;
    }
}