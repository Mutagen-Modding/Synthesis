using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Order;
using Noggog;
using Synthesis.Bethesda.Execution.Groups;

namespace Synthesis.Bethesda.Execution.Running.Runner;

public interface ILoadOrderForRunProvider
{
    IList<IModListingGetter> Get(ModKey modKey, IReadOnlySet<ModKey> blacklist);
}

public class LoadOrderForRunProvider : ILoadOrderForRunProvider
{
    public IGroupLoadOrderProvider LoadOrderListingsProvider { get; }

    public LoadOrderForRunProvider(
        IGroupLoadOrderProvider loadOrderListingsProvider)
    {
        LoadOrderListingsProvider = loadOrderListingsProvider;
    }
        
    public IList<IModListingGetter> Get(ModKey modKey, IReadOnlySet<ModKey> blacklist)
    {
        // Copy plugins text to working directory, trimming synthesis and anything after
        var loadOrderList = LoadOrderListingsProvider.Get(blacklist).ToList();
        var trimIndex = loadOrderList.IndexOf(modKey, (listing, key) => listing.ModKey == key);
        if (trimIndex != -1)
        {
            loadOrderList.RemoveToCount(trimIndex);
        }

        return loadOrderList;
    }
}