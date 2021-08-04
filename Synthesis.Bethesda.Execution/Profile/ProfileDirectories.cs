using System.IO;
using Noggog;
using Synthesis.Bethesda.Execution.Pathing;

namespace Synthesis.Bethesda.Execution.Profile
{
    public interface IProfileDirectories
    {
        DirectoryPath ProfileDirectory { get; }
        DirectoryPath WorkingDirectory { get; }
    }

    public class ProfileDirectories : IProfileDirectories
    {
        public IWorkingDirectoryProvider Paths { get; }
        public IWorkingDirectorySubPaths WorkingDirectorySubPaths { get; }
        public IProfileIdentifier Ident { get; }

        public DirectoryPath ProfileDirectory => Path.Combine(Paths.WorkingDirectory, Ident.ID);
        public DirectoryPath WorkingDirectory => WorkingDirectorySubPaths.ProfileWorkingDirectory(Ident.ID);

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
}