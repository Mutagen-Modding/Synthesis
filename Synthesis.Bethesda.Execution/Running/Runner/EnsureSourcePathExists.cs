using System.IO.Abstractions;
using Noggog;

namespace Synthesis.Bethesda.Execution.Running.Runner;

public interface IEnsureSourcePathExists
{
    void Ensure(FilePath? sourcePath);
}

public class EnsureSourcePathExists : IEnsureSourcePathExists
{
    public IFileSystem FileSystem { get; }

    public EnsureSourcePathExists(
        IFileSystem fileSystem)
    {
        FileSystem = fileSystem;
    }
        
    public void Ensure(FilePath? sourcePath)
    {
        if (sourcePath != null)
        {
            if (!FileSystem.File.Exists(sourcePath))
            {
                throw new FileNotFoundException($"Source path did not exist: {sourcePath}");
            }
        }
    }
}