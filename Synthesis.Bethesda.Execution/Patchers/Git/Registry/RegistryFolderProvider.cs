using Noggog;
using Synthesis.Bethesda.Execution.Pathing;

namespace Synthesis.Bethesda.Execution.Patchers.Git.Registry;

public interface IRegistryFolderProvider
{
    DirectoryPath RegistryFolder { get; }
}

public class RegistryFolderProvider : IRegistryFolderProvider
{
    public DirectoryPath RegistryFolder { get; }

    public RegistryFolderProvider(
        IWorkingDirectoryProvider workingDirectoryProvider)
    {
        RegistryFolder = Path.Combine(workingDirectoryProvider.WorkingDirectory, "Registry");
    }
}