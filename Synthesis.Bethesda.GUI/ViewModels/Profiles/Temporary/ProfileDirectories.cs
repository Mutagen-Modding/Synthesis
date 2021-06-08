using System.IO;

namespace Synthesis.Bethesda.GUI.Temporary
{
    public class ProfileDirectories
    {
        public string ProfileDirectory { get; }
        public string WorkingDirectory { get; }

        public ProfileDirectories(ProfileIdentifier ident)
        {
            ProfileDirectory = Path.Combine(Execution.Paths.WorkingDirectory, ident.ID);
            WorkingDirectory = Execution.Paths.ProfileWorkingDirectory(ident.ID);
        }
    }
}