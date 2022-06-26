using System.Diagnostics.CodeAnalysis;
using Noggog;

namespace Synthesis.Bethesda.Execution.Pathing;

public interface IWorkingDirectorySubPaths
{
    DirectoryPath LoadingFolder { get; }
    DirectoryPath ProfileWorkingDirectory(string id);
}

[ExcludeFromCodeCoverage]
public class WorkingDirectorySubPaths : IWorkingDirectorySubPaths
{
    public IWorkingDirectoryProvider WorkingDir { get; }
    public DirectoryPath LoadingFolder => Path.Combine(WorkingDir.WorkingDirectory, "Loading");
    public DirectoryPath ProfileWorkingDirectory(string id) => Path.Combine(WorkingDir.WorkingDirectory, id, "Workspace");

    public WorkingDirectorySubPaths(
        IWorkingDirectoryProvider workingDir)
    {
        WorkingDir = workingDir;
    }
}