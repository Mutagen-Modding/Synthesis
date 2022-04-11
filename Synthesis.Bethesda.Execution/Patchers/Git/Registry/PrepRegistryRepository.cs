using System.Threading;
using Noggog;
using Synthesis.Bethesda.Execution.GitRepository;

namespace Synthesis.Bethesda.Execution.Patchers.Git.Registry;

public interface IPrepRegistryRepository
{
    ErrorResponse Prep(CancellationToken cancellationToken);
}

public class PrepRegistryRepository : IPrepRegistryRepository
{
    public IResetToLatestMain ResetToLatestMain { get; }
    public IRemoteRegistryUrlProvider RegistryUrlProvider { get; }
    public ICheckOrCloneRepo CheckOrClone { get; }
    public IProvideRepositoryCheckouts RepositoryCheckouts { get; }
    public IRegistryFolderProvider RegistryFolderProvider { get; }

    public PrepRegistryRepository(
        ICheckOrCloneRepo checkOrClone,
        IRemoteRegistryUrlProvider registryUrlProvider,
        IProvideRepositoryCheckouts repositoryCheckouts,
        IResetToLatestMain resetToLatestMain,
        IRegistryFolderProvider registryFolderProvider)
    {
        ResetToLatestMain = resetToLatestMain;
        RegistryUrlProvider = registryUrlProvider;
        CheckOrClone = checkOrClone;
        RepositoryCheckouts = repositoryCheckouts;
        RegistryFolderProvider = registryFolderProvider;
    }
        
    public ErrorResponse Prep(CancellationToken cancellationToken)
    {
        var localRepoPath = CheckOrClone.Check(
            RegistryUrlProvider.Url,
            RegistryFolderProvider.RegistryFolder,
            cancellationToken);
        if (localRepoPath.Failed) return localRepoPath;
            
        using var repoCheckout = RepositoryCheckouts.Get(localRepoPath.Value.Local);
        var repo = repoCheckout.Repository;

        return ResetToLatestMain.TryReset(repo);
    }
}