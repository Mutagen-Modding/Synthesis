using System.IO;
using Synthesis.Bethesda.Execution;

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
            IPaths paths,
            IProfileIdentifier ident)
        {
            ProfileDirectory = Path.Combine(paths.WorkingDirectory, ident.ID);
            WorkingDirectory = paths.ProfileWorkingDirectory(ident.ID);
        }
    }
}