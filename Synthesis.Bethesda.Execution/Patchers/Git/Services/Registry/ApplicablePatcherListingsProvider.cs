using Mutagen.Bethesda.Environments.DI;
using Noggog;
using Synthesis.Bethesda.DTO;

namespace Synthesis.Bethesda.Execution.Patchers.Git.Services.Registry;

public class ApplicablePatcherListingsProvider
{
    private readonly IRegistryListingsProvider _listingsProvider;
    private readonly IGameReleaseContext _gameReleaseContext;
    private readonly TargetedReleasesFromListingRetriever _releasesFromListingRetriever;

    public ApplicablePatcherListingsProvider(
        IRegistryListingsProvider listingsProvider,
        IGameReleaseContext gameReleaseContext,
        TargetedReleasesFromListingRetriever releasesFromListingRetriever)
    {
        _listingsProvider = listingsProvider;
        _gameReleaseContext = gameReleaseContext;
        _releasesFromListingRetriever = releasesFromListingRetriever;
    }

    public GetResponse<IReadOnlyList<(RepositoryListing Repository, PatcherListing Patcher)>> Get(CancellationToken cancel)
    {
        var repoListings = _listingsProvider.Get(cancel);
        if (repoListings.Failed) return repoListings.BubbleFailure<IReadOnlyList<(RepositoryListing Repository, PatcherListing Patcher)>>();
        List<(RepositoryListing Repository, PatcherListing Patcher)> filtered = new();
        foreach (var repoListing in repoListings.Value)
        {
            foreach (var patcherListing in repoListing.Patchers)
            {
                var targetedReleases = _releasesFromListingRetriever.Get(patcherListing);
                if (targetedReleases.Contains(_gameReleaseContext.Release))
                {
                    filtered.Add((repoListing, patcherListing));
                }
            }
        }

        return filtered;
    }
}