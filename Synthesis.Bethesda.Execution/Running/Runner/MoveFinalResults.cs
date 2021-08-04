using System.IO.Abstractions;
using Mutagen.Bethesda.Plugins;
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
        public IFileSystem FileSystem { get; }

        public MoveFinalResults(
            IRunReporter reporter,
            IFileSystem fileSystem)
        {
            _reporter = reporter;
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
            
            FileSystem.File.Copy(finalPatch.Path, outputPath);
            _reporter.Write(default!, default, $"Exported patch to: {outputPath}");
        }
    }
}