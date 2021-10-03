using System.Collections.Generic;
using System.Linq;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Plugins.Order.DI;
using Noggog;

namespace Mutagen.Bethesda.Synthesis.States
{
    public interface IGetStateLoadOrder
    {
        IEnumerable<IModListingGetter> GetLoadOrder(PatcherPreferences? userPrefs = null);
    }

    public class GetStateLoadOrder : IGetStateLoadOrder
    {
        private readonly IImplicitListingsProvider _implicitListing;
        private readonly IPluginListingsProvider _pluginListings;

        public GetStateLoadOrder(
            IImplicitListingsProvider implicitListing,
            IPluginListingsProvider pluginListings)
        {
            _implicitListing = implicitListing;
            _pluginListings = pluginListings;
        }
        
        public IEnumerable<IModListingGetter> GetLoadOrder(PatcherPreferences? userPrefs = null)
        {
            // This call will implicitly get Creation Club entries, too, as the Synthesis systems should be merging
            // things into a singular load order file for consumption here
            var loadOrderListing = _implicitListing.Get();
            loadOrderListing = loadOrderListing.Concat(_pluginListings.Get());
            loadOrderListing = loadOrderListing.Distinct(x => x.ModKey);
            if (userPrefs?.InclusionMods != null)
            {
                var inclusions = userPrefs.InclusionMods.ToHashSet();
                loadOrderListing = loadOrderListing
                    .Where(m => inclusions.Contains(m.ModKey));
            }
            if (userPrefs?.ExclusionMods != null)
            {
                var exclusions = userPrefs.ExclusionMods.ToHashSet();
                loadOrderListing = loadOrderListing
                    .Where(m => !exclusions.Contains(m.ModKey));
            }
            return loadOrderListing;
        }
    }
}