using System.IO.Abstractions;
using Newtonsoft.Json;
using Noggog;
using Synthesis.Bethesda.Execution.Settings.V1;

namespace Synthesis.Bethesda.Execution.Settings.Json.Pipeline.V1
{
    public interface IPipelineSettingsV1Reader
    {
        PipelineSettings Read(FilePath path);
    }

    public class PipelineSettingsV1Reader : IPipelineSettingsV1Reader
    {
        private readonly IFileSystem _fileSystem;

        public PipelineSettingsV1Reader(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }
        
        public PipelineSettings Read(FilePath path)
        {
            var text = _fileSystem.File.ReadAllText(path);
            text = text.Replace(
                "Synthesis.Bethesda.Execution.Settings.SynthesisProfile",
                "Synthesis.Bethesda.Execution.Settings.V1.SynthesisProfile");
            return JsonConvert.DeserializeObject<PipelineSettings>(
                text,
                Constants.JsonSettings)!;
        }
    }
}