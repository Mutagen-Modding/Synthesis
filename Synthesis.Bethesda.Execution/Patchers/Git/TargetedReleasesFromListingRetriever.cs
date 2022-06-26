using Mutagen.Bethesda;
using Synthesis.Bethesda.DTO;

namespace Synthesis.Bethesda.Execution.Patchers.Git;

public class TargetedReleasesFromListingRetriever
{
    public IReadOnlyCollection<GameRelease> Get(PatcherListing listing)
    {
        if (listing.Customization?.TargetedReleases.Length > 0)
        {
            return listing.Customization.TargetedReleases;
        }

        return listing.IncludedLibraries.SelectMany(x => x.GetRelatedReleases()).ToList();
    }
}