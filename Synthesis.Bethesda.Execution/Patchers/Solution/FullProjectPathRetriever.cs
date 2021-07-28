using System.IO.Abstractions;
using System.Linq;
using Noggog;

namespace Synthesis.Bethesda.Execution.Patchers.Solution
{
    public record ProjectPaths(FilePath FullPath, string SubPath);
    
    public interface IFullProjectPathRetriever
    {
        ProjectPaths? Get(FilePath solutionPath, string projSubpath);
    }

    public class FullProjectPathRetriever : IFullProjectPathRetriever
    {
        private readonly IFileSystem _fileSystem;
        private readonly IAvailableProjectsRetriever _AvailableProjectsRetriever;

        public FullProjectPathRetriever(
            IFileSystem fileSystem,
            IAvailableProjectsRetriever availableProjectsRetriever)
        {
            _fileSystem = fileSystem;
            _AvailableProjectsRetriever = availableProjectsRetriever;
        }
        
        public ProjectPaths? Get(FilePath solutionPath, string projSubpath)
        {
            var projName = _fileSystem.Path.GetFileName(projSubpath);
            var str = _AvailableProjectsRetriever.Get(solutionPath)
                .FirstOrDefault(av => _fileSystem.Path.GetFileName(av).Equals(projName));
            if (str == null) return null;
            return new ProjectPaths(new FilePath(str), str);
        }
    }
}