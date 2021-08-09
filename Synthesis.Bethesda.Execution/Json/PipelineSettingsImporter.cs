using System.IO.Abstractions;
using Newtonsoft.Json;
using Noggog;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.Execution.Json
{
    public interface IPipelineSettingsImporter
    {
        PipelineSettings Import(FilePath path);
    }

    public class PipelineSettingsImporter : IPipelineSettingsImporter
    {
        private readonly IFileSystem _fileSystem;

        public PipelineSettingsImporter(
            IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public PipelineSettings Import(FilePath path)
        {
            return JsonConvert.DeserializeObject<PipelineSettings>(
                _fileSystem.File.ReadAllText(path),
                Constants.JsonSettings)!;
        }
    }
}