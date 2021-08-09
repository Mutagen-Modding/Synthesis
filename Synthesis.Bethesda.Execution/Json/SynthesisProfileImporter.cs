using System.IO.Abstractions;
using Newtonsoft.Json;
using Noggog;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.Execution.Json
{
    public interface ISynthesisProfileImporter
    {
        SynthesisProfile Import(FilePath path);
    }

    public class SynthesisProfileImporter : ISynthesisProfileImporter
    {
        private readonly IFileSystem _fileSystem;

        public SynthesisProfileImporter(
            IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public SynthesisProfile Import(FilePath path)
        {
            return JsonConvert.DeserializeObject<SynthesisProfile>(
                _fileSystem.File.ReadAllText(path),
                Constants.JsonSettings)!;
        }
    }
}