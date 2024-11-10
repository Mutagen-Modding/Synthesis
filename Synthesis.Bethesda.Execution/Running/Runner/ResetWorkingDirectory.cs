using System.IO.Abstractions;
using Noggog.IO;
using Synthesis.Bethesda.Execution.Profile;
using Synthesis.Bethesda.Execution.Profile.Services;

namespace Synthesis.Bethesda.Execution.Running.Runner;

public interface IResetWorkingDirectory
{
    void Reset();
}

public class ResetWorkingDirectory : IResetWorkingDirectory
{
    public IFileSystem FileSystem { get; }
    public IDeleteEntireDirectory DeleteEntireDirectory { get; }
    public IProfileDirectories ProfileDirectories { get; }

    public ResetWorkingDirectory(
        IFileSystem fileSystem,
        IDeleteEntireDirectory deleteEntireDirectory,
        IProfileDirectories profileDirectories)
    {
        FileSystem = fileSystem;
        DeleteEntireDirectory = deleteEntireDirectory;
        ProfileDirectories = profileDirectories;
    }
        
    public void Reset()
    {
        var workingDirectory = ProfileDirectories.WorkingDirectory;
        DeleteEntireDirectory.DeleteEntireFolder(workingDirectory);
        FileSystem.Directory.CreateDirectory(workingDirectory);
    }
}