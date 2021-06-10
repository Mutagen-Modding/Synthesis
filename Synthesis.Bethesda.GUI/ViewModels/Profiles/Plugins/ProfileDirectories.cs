using System.IO;

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

        public ProfileDirectories(IProfileIdentifier ident)
        {
            ProfileDirectory = Path.Combine(Execution.Paths.WorkingDirectory, ident.ID);
            WorkingDirectory = Execution.Paths.ProfileWorkingDirectory(ident.ID);
        }
    }
}