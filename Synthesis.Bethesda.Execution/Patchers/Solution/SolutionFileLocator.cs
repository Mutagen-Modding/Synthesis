using System.IO.Abstractions;
using Noggog;

namespace Synthesis.Bethesda.Execution.Patchers.Solution;

public interface ISolutionFileLocator
{
    FilePath? GetPath(DirectoryPath repositoryPath);
}

public class SolutionFileLocator : ISolutionFileLocator
{
    private readonly IFileSystem _fs;

    public SolutionFileLocator(IFileSystem fs)
    {
        _fs = fs;
    }
        
    public FilePath? GetPath(DirectoryPath repositoryPath)
    {
        return _fs.Directory.EnumerateFiles(repositoryPath.Path, "*.sln")
            .Select<string, FilePath?>(x => new FilePath(x))
            .FirstOrDefault();
    }
}