using Noggog;
using Synthesis.Bethesda.Execution.Groups;
using Synthesis.Bethesda.Execution.Profile.Services;

namespace Synthesis.Bethesda.Execution.Running.Runner;

public interface IRunLoadOrderPathProvider
{
    FilePath PathFor(IGroupRun groupRun);
}

public class RunLoadOrderPathProvider : IRunLoadOrderPathProvider
{
    public IProfileDirectories ProfileDirectories { get; }

    public RunLoadOrderPathProvider(
        IProfileDirectories profileDirectories)
    {
        ProfileDirectories = profileDirectories;
    }

    public FilePath PathFor(IGroupRun groupRun)
    {
        return System.IO.Path.Combine(ProfileDirectories.WorkingDirectory, groupRun.ModKey.Name, "Plugins.txt");
    }
}