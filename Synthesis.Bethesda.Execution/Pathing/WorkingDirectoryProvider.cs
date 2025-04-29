using Noggog;
using Noggog.IO;

namespace Synthesis.Bethesda.Execution.Pathing;

public interface IWorkingDirectoryProvider
{
    DirectoryPath WorkingDirectory { get; }
}

public class WorkingDirectoryProvider : IWorkingDirectoryProvider
{
    private readonly ICurrentDirectoryProvider _currentDirectoryProvider;
    public IEnvironmentTemporaryDirectoryProvider TempDir { get; }

    public WorkingDirectoryProvider(
        ICurrentDirectoryProvider currentDirectoryProvider, 
        IEnvironmentTemporaryDirectoryProvider tempDir)
    {
        _currentDirectoryProvider = currentDirectoryProvider;
        TempDir = tempDir;
        #if DEBUG
        WorkingDirectory = Path.Combine(TempDir.Path, "Synthesis")!;
        #else
        WorkingDirectory = Path.Combine(_currentDirectoryProvider.CurrentDirectory, "Workspace")!;
        #endif
    }
        
    public DirectoryPath WorkingDirectory { get; }
}