using System.IO;
using System.IO.Abstractions;
using System.Linq;
using Noggog;

namespace Synthesis.Bethesda.Execution.Patchers.Solution
{
    public interface ISolutionFileLocator
    {
        string? GetPath(DirectoryPath repositoryPath);
    }

    public class SolutionFileLocator : ISolutionFileLocator
    {
        private readonly IFileSystem _fs;

        public SolutionFileLocator(IFileSystem fs)
        {
            _fs = fs;
        }
        
        public string? GetPath(DirectoryPath repositoryPath)
        {
            return _fs.Directory.EnumerateFiles(repositoryPath.Path, "*.sln").FirstOrDefault();
        }
    }
}