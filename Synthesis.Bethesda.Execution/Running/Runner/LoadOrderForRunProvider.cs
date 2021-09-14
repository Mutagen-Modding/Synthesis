using System.Collections.Generic;
using System.Linq;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Order;
using Noggog;
using Synthesis.Bethesda.Execution.Profile;

namespace Synthesis.Bethesda.Execution.Running.Runner
{
    public interface ILoadOrderForRunProvider
    {
        IList<IModListingGetter> Get(ModKey modKey);
    }

    public class LoadOrderForRunProvider : ILoadOrderForRunProvider
    {
        public IProfileLoadOrderProvider LoadOrderListingsProvider { get; }

        public LoadOrderForRunProvider(
            IProfileLoadOrderProvider loadOrderListingsProvider)
        {
            LoadOrderListingsProvider = loadOrderListingsProvider;
        }
        
        public IList<IModListingGetter> Get(ModKey modKey)
        {
            // Copy plugins text to working directory, trimming synthesis and anything after
            var loadOrderList = LoadOrderListingsProvider.Get().ToList();
            var trimIndex = loadOrderList.IndexOf(modKey, (listing, key) => listing.ModKey == key);
            if (trimIndex != -1)
            {
                loadOrderList.RemoveToCount(trimIndex);
            }

            return loadOrderList;
        }
    }
}