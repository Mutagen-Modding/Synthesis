using Noggog;
using Synthesis.Bethesda.Execution.Profile;

namespace Synthesis.Bethesda.Execution.Running.Runner
{
    public interface IRunLoadOrderPathProvider
    {
        FilePath Path { get; }
    }

    public class RunLoadOrderPathProvider : IRunLoadOrderPathProvider
    {
        public IProfileDirectories ProfileDirectories { get; }
        public FilePath Path => System.IO.Path.Combine(ProfileDirectories.WorkingDirectory, "Plugins.txt");

        public RunLoadOrderPathProvider(
            IProfileDirectories profileDirectories)
        {
            ProfileDirectories = profileDirectories;
        }
    }
}