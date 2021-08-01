using System.IO;
using Synthesis.Bethesda.Execution.Pathing;

namespace Synthesis.Bethesda.Execution.Profile
{
    public interface IProfileDirectories
    {
        string ProfileDirectory { get; }
        string WorkingDirectory { get; }
    }

    public class ProfileDirectories : IProfileDirectories
    {
        public IWorkingDirectoryProvider Paths { get; }
        public IWorkingDirectorySubPaths WorkingDirectorySubPaths { get; }
        public IProfileIdentifier Ident { get; }

        public string ProfileDirectory => Path.Combine(Paths.WorkingDirectory, Ident.ID);
        public string WorkingDirectory => WorkingDirectorySubPaths.ProfileWorkingDirectory(Ident.ID);

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