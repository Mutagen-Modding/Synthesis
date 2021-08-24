using System.IO;
using System.IO.Abstractions;
using Noggog;

namespace Synthesis.Bethesda.Execution.Settings.Json.Pipeline
{
    public interface IPipelineSettingsBackup
    {
        void Backup(int readVersion, FilePath path);
    }

    public class PipelineSettingsBackup : IPipelineSettingsBackup
    {
        private readonly IFileSystem _fileSystem;

        public PipelineSettingsBackup(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }
        
        public void Backup(int readVersion, FilePath path)
        {
            var outPath = Path.Combine(path.Directory!.Value.Path, $"{path.NameWithoutExtension}.v{readVersion}.json");
            _fileSystem.File.Copy(path, outPath, overwrite: true);
        }
    }
}