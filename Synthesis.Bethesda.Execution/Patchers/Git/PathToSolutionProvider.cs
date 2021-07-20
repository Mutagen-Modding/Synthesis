using System.IO;
using System.IO.Abstractions;
using System.Linq;

namespace Synthesis.Bethesda.Execution.Patchers.Git
{
    public interface IPathToSolutionProvider
    {
        string? Path { get; }
    }

    public class PathToSolutionProvider : IPathToSolutionProvider
    {
        private readonly IFileSystem _fs;
        private readonly IDriverRepoDirectoryProvider _pathToRepo;

        public PathToSolutionProvider(
            IFileSystem fs,
            IDriverRepoDirectoryProvider pathToRepo)
        {
            _fs = fs;
            _pathToRepo = pathToRepo;
        }
        
        public string? Path
        {
            get
            {
                return _fs.Directory.EnumerateFiles(_pathToRepo.Path, "*.sln").FirstOrDefault();
            }
        }
    }
}