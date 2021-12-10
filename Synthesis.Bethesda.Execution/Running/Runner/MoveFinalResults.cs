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
            DirectoryPath outputPath);
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
            DirectoryPath outputPath)
        {
            if (!FileSystem.Directory.Exists(outputPath))
            {
                FileSystem.Directory.CreateDirectory(outputPath);
            }
            
            var finalPathFolder = finalPatch.Directory;
            
            FileSystem.Directory.DeepCopy(finalPathFolder!.Value.Path, outputPath);
            _reporter.Write(default!, default, $"Exported patch to workspace: {outputPath}");
            FileSystem.Directory.DeepCopy(finalPathFolder.Value.Path, _dataDirectoryProvider.Path, overwrite: true);
            _reporter.Write(default!, default, $"Exported patch to final destination: {outputPath}");
        }
    }
}