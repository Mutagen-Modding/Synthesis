using System.IO;
using Synthesis.Bethesda.Execution;
using Synthesis.Bethesda.Execution.Pathing;

namespace Synthesis.Bethesda.GUI.Profiles.Plugins
{
    public interface IProfileDirectories
    {
        string ProfileDirectory { get; }
        string WorkingDirectory { get; }
    }

    public class ProfileDirectories : IProfileDirectories
    {
        public string ProfileDirectory { get; }
        public string WorkingDirectory { get; }

        public ProfileDirectories(
            IProvideWorkingDirectory paths,
            IWorkingDirectorySubPaths workingDirectorySubPaths,
            IProfileIdentifier ident)
        {
            ProfileDirectory = Path.Combine(paths.WorkingDirectory, ident.ID);
            WorkingDirectory = workingDirectorySubPaths.ProfileWorkingDirectory(ident.ID);
        }
    }
}