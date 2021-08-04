using System.Collections.Generic;
using System.Linq;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Plugins.Order.DI;
using Noggog;

namespace Synthesis.Bethesda.Execution.Running.Runner
{
    public interface ILoadOrderForRunProvider
    {
        IList<IModListingGetter> Get(ModPath outputPath);
    }

    public class LoadOrderForRunProvider : ILoadOrderForRunProvider
    {
        public ILoadOrderListingsProvider LoadOrderListingsProvider { get; }

        public LoadOrderForRunProvider(
            ILoadOrderListingsProvider loadOrderListingsProvider)
        {
            LoadOrderListingsProvider = loadOrderListingsProvider;
        }
        
        public IList<IModListingGetter> Get(ModPath outputPath)
        {
            // Copy plugins text to working directory, trimming synthesis and anything after
            var loadOrderList = LoadOrderListingsProvider.Get().ToList();
            var trimIndex = loadOrderList.IndexOf(outputPath.ModKey, (listing, key) => listing.ModKey == key);
            if (trimIndex != -1)
            {
                loadOrderList.RemoveToCount(trimIndex);
            }

            return loadOrderList;
        }
    }
}