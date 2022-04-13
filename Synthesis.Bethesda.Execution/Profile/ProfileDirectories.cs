using System.Diagnostics.CodeAnalysis;
using System.IO;
using Noggog;
using Synthesis.Bethesda.Execution.Pathing;

namespace Synthesis.Bethesda.Execution.Profile;

public interface IProfileDirectories
{
    DirectoryPath ProfileDirectory { get; }
    DirectoryPath WorkingDirectory { get; }
    DirectoryPath OutputDirectory { get; }
}

public class ProfileDirectories : IProfileDirectories
{
    public IWorkingDirectoryProvider Paths { get; }
    public IWorkingDirectorySubPaths WorkingDirectorySubPaths { get; }
    public IProfileIdentifier Ident { get; }

    public DirectoryPath ProfileDirectory => Path.Combine(Paths.WorkingDirectory, Ident.ID);
    public DirectoryPath WorkingDirectory => WorkingDirectorySubPaths.ProfileWorkingDirectory(Ident.ID);
    public DirectoryPath OutputDirectory => Path.Combine(WorkingDirectory, "Output");

    [ExcludeFromCodeCoverage]
    public ProfileDirectories(
        IWorkingDirectoryProvider paths,
        IWorkingDirectorySubPaths workingDirectorySubPaths,
        IProfileIdentifier ident)
    {
        Paths = paths;
        WorkingDirectorySubPaths = workingDirectorySubPaths;
        Ident = ident;
    }
}