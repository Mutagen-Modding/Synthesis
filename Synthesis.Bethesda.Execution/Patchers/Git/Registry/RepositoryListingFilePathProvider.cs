using Noggog;

namespace Synthesis.Bethesda.Execution.Patchers.Git.Registry;

public interface IRepositoryListingFilePathProvider
{
    FilePath Path { get; }
}

public class RepositoryListingFilePathProvider : IRepositoryListingFilePathProvider
{
    private readonly IRegistryFolderProvider _registryFolderProvider;

    public RepositoryListingFilePathProvider(
        IRegistryFolderProvider registryFolderProvider)
    {
        _registryFolderProvider = registryFolderProvider;
    }
        
    public FilePath Path => System.IO.Path.Combine(
        _registryFolderProvider.RegistryFolder, 
        Synthesis.Bethesda.Constants.AutomaticListingFileName);
}