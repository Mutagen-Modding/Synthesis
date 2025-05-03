using Noggog;
using Noggog.IO;

namespace Synthesis.Bethesda.Execution.Pathing;

public interface IWorkingDirectoryProvider
{
    DirectoryPath WorkingDirectory { get; }
}

public class WorkingDirectoryProvider : IWorkingDirectoryProvider
{
    public IEnvironmentTemporaryDirectoryProvider TempDir { get; }

    public WorkingDirectoryProvider(IEnvironmentTemporaryDirectoryProvider tempDir)
    {
        TempDir = tempDir;
    }
        
    public DirectoryPath WorkingDirectory => Path.Combine(TempDir.Path, "Synthesis")!;
}