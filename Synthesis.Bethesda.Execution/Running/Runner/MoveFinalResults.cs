using System.IO;
using System.IO.Abstractions;
using Mutagen.Bethesda.Environments.DI;
using Noggog;
using Synthesis.Bethesda.Execution.Reporters;

namespace Synthesis.Bethesda.Execution.Running.Runner
{
    public interface IMoveFinalResults
    {
        void Move(
            FilePath finalPatch,
            FilePath outputPath);
    }

    public class MoveFinalResults : IMoveFinalResults
    {
        private readonly IRunReporter _reporter;
        private readonly IDataDirectoryProvider _dataDirectoryProvider;
        public IFileSystem FileSystem { get; }

        public MoveFinalResults(
            IRunReporter reporter,
            IDataDirectoryProvider dataDirectoryProvider,
            IFileSystem fileSystem)
        {
            _reporter = reporter;
            _dataDirectoryProvider = dataDirectoryProvider;
            FileSystem = fileSystem;
        }
        
        public void Move(
            FilePath finalPatch,
            FilePath outputPath)
        {
            if (FileSystem.File.Exists(outputPath))
            {
                FileSystem.File.Delete(outputPath);
            }

            if (!FileSystem.Directory.Exists(outputPath.Directory))
            {
                FileSystem.Directory.CreateDirectory(outputPath.Directory);
            }
            
            var dataFolderPath = Path.Combine(_dataDirectoryProvider.Path, outputPath.Name);
            
            FileSystem.File.Copy(finalPatch.Path, outputPath);
            _reporter.Write(default!, default, $"Exported patch to workspace: {outputPath}");
            FileSystem.File.Copy(finalPatch.Path, dataFolderPath, overwrite: true);
            _reporter.Write(default!, default, $"Exported patch to final destination: {outputPath}");
        }
    }
}