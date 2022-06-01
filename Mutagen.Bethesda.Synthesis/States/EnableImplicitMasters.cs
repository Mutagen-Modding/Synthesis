using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Plugins.Order.DI;

namespace Mutagen.Bethesda.Synthesis.States;

public interface IEnableImplicitMasters
{
    void Add(IList<ILoadOrderListingGetter> loadOrderListing);
}

public class EnableImplicitMasters : IEnableImplicitMasters
{
    private readonly IFindImplicitlyIncludedMods _findImplicitlyIncludedMods;

    public EnableImplicitMasters(
        IFindImplicitlyIncludedMods findImplicitlyIncludedMods)
    {
        _findImplicitlyIncludedMods = findImplicitlyIncludedMods;
    }

    public void Add(IList<ILoadOrderListingGetter> loadOrderListing)
    {
        var implicitlyAdded = _findImplicitlyIncludedMods.Find(loadOrderListing, skipMissingMods: true)
            .ToHashSet();
        for (int i = loadOrderListing.Count - 1; i >= 0; i--)
        {
            var listing = loadOrderListing[i];
            if (!listing.Enabled && implicitlyAdded.Contains(listing.ModKey))
            {
                loadOrderListing[i] = new LoadOrderListing(listing.ModKey, enabled: true);
            }
        }
    }
}