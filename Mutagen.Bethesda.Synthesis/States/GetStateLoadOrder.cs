using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Plugins.Order.DI;
using Mutagen.Bethesda.Synthesis.States.DI;
using Noggog;

namespace Mutagen.Bethesda.Synthesis.States;

public interface IGetStateLoadOrder
{
    GetStateLoadOrder.LoadOrderReturn GetFinalLoadOrder(
        GameRelease gameRelease,
        ModKey exportKey,
        string dataFolderPath,
        bool addCcMods,
        PatcherPreferences userPrefs);
}

public class GetStateLoadOrder : IGetStateLoadOrder
{
    private readonly IImplicitListingsProvider _implicitListing;
    private readonly IOrderListings _orderListings;
    private readonly ICreationClubListingsProvider _ccListingsProvider;
    private readonly IPluginListingsProvider _pluginListings;
    private readonly IEnableImplicitMastersFactory _enableImplicitMasters;

    public GetStateLoadOrder(
        IImplicitListingsProvider implicitListing,
        IOrderListings orderListings,
        ICreationClubListingsProvider ccListingsProvider,
        IPluginListingsProvider pluginListings,
        IEnableImplicitMastersFactory enableImplicitMasters)
    {
        _implicitListing = implicitListing;
        _orderListings = orderListings;
        _ccListingsProvider = ccListingsProvider;
        _pluginListings = pluginListings;
        _enableImplicitMasters = enableImplicitMasters;
    }
        
    public IEnumerable<ILoadOrderListingGetter> GetUnfilteredLoadOrder(bool addCcMods, PatcherPreferences? userPrefs = null)
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
            var loadOrderListing = _implicitListing.Get().Cast<ILoadOrderListingGetter>();
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

    public record LoadOrderReturn(
        IList<ILoadOrderListingGetter> ProcessedLoadOrder,
        ExtendedList<LoadOrderListing> Raw);
    
    public LoadOrderReturn GetFinalLoadOrder(
        GameRelease gameRelease,
        ModKey exportKey,
        string dataFolderPath,
        bool addCcMods,
        PatcherPreferences userPrefs)
    {
        // Get load order
        var loadOrderListing = GetUnfilteredLoadOrder(addCcMods, userPrefs)
            .ToExtendedList();
        var rawLoadOrder = loadOrderListing.Select(x => new LoadOrderListing(x.ModKey, x.Enabled)).ToExtendedList();

        // Trim past export key
        var synthIndex = loadOrderListing.IndexOf(exportKey, (listing, key) => listing.ModKey == key);
        if (synthIndex != -1)
        {
            loadOrderListing.RemoveToCount(synthIndex);
        }

        if (userPrefs.AddImplicitMasters)
        {
            _enableImplicitMasters
                .Get(dataFolderPath, gameRelease)
                .Add(loadOrderListing);
        }

        // Remove disabled mods
        if (!userPrefs.IncludeDisabledMods)
        {
            loadOrderListing = loadOrderListing.OnlyEnabled().ToExtendedList();
        }

        return new LoadOrderReturn(
            loadOrderListing,
            rawLoadOrder);
    }
}