using System.Collections.Generic;
using System.Linq;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Plugins.Order.DI;
using Noggog;

namespace Mutagen.Bethesda.Synthesis.States
{
    public interface IGetStateLoadOrder
    {
        IEnumerable<IModListingGetter> GetLoadOrder(bool addCcMods, PatcherPreferences? userPrefs = null);
    }

    public class GetStateLoadOrder : IGetStateLoadOrder
    {
        private readonly IImplicitListingsProvider _implicitListing;
        private readonly IOrderListings _orderListings;
        private readonly ICreationClubListingsProvider _ccListingsProvider;
        private readonly IPluginListingsProvider _pluginListings;

        public GetStateLoadOrder(
            IImplicitListingsProvider implicitListing,
            IOrderListings orderListings,
            ICreationClubListingsProvider ccListingsProvider,
            IPluginListingsProvider pluginListings)
        {
            _implicitListing = implicitListing;
            _orderListings = orderListings;
            _ccListingsProvider = ccListingsProvider;
            _pluginListings = pluginListings;
        }
        
        public IEnumerable<IModListingGetter> GetLoadOrder(bool addCcMods, PatcherPreferences? userPrefs = null)
        {
            if (addCcMods)
            {
                var implicitMods = _implicitListing.Get();

                var ccMods = _ccListingsProvider.Get();
                
                var loadOrderListing = _pluginListings.Get();
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

                return _orderListings.Order(
                    implicitListings: implicitMods,
                    pluginsListings: loadOrderListing,
                    creationClubListings: ccMods,
                    selector: x => x.ModKey);
            }
            else
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
}