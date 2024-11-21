using Noggog;
using Noggog.GitRepository;
using Synthesis.Bethesda.Execution.GitRepository;

namespace Synthesis.Bethesda.Execution.Patchers.Git.Services.Registry;

public interface IPrepRegistryRepository
{
    ErrorResponse Prep(CancellationToken cancellationToken);
}

public class PrepRegistryRepository : IPrepRegistryRepository
{
    private readonly ICheckIfRepositoryDesirable _repositoryDesirable;
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
        ICheckIfRepositoryDesirable repositoryDesirable,
        IRegistryFolderProvider registryFolderProvider)
    {
        _repositoryDesirable = repositoryDesirable;
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
            isDesirable: _repositoryDesirable.IsDesirable,
            cancellationToken);
        if (localRepoPath.Failed) return localRepoPath;
            
        using var repoCheckout = RepositoryCheckouts.Get(localRepoPath.Value.Local);
        var repo = repoCheckout.Repository;

        return ResetToLatestMain.TryReset(repo);
    }
}