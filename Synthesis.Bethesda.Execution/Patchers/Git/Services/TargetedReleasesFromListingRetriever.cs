using Mutagen.Bethesda;
using Synthesis.Bethesda.DTO;

namespace Synthesis.Bethesda.Execution.Patchers.Git.Services;

public class TargetedReleasesFromListingRetriever
{
    // No real patcher targets releases across all of these unrelated game families at once.
    // Older Synthesis versions defaulted every targeting checkbox to on, which wrote out a
    // meta file listing every release.  When we see that spread we treat it as "no opinion".
    private static readonly GameCategory[] DistinctFamilies =
    {
        GameCategory.Oblivion,
        GameCategory.Skyrim,
        GameCategory.Fallout4,
    };

    public IReadOnlyCollection<GameRelease> Get(PatcherListing listing)
    {
        var targeted = listing.Customization?.TargetedReleases;
        if (targeted is { Length: > 0 } && !IsLegacySelectAll(targeted))
        {
            return targeted;
        }

        return listing.IncludedLibraries.SelectMany(x => x.GetRelatedReleases()).ToList();
    }

    private static bool IsLegacySelectAll(IReadOnlyCollection<GameRelease> targeted)
    {
        // Spanning every distinct game family is the fingerprint of the old "select all"
        // default rather than a deliberate choice.  Match on family coverage (not an exact
        // release set) so newly added releases don't break the heuristic for old meta files.
        return DistinctFamilies.All(category =>
            category.GetRelatedReleases().Any(targeted.Contains));
    }
}
