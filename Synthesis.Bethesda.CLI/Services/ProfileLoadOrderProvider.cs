using System.Collections.Generic;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Plugins.Order.DI;
using Synthesis.Bethesda.Execution.Profile;

namespace Synthesis.Bethesda.CLI.Services
{
    public class ProfileLoadOrderProvider : IProfileLoadOrderProvider
    {
        private readonly ILoadOrderListingsProvider _listingsProvider;

        public ProfileLoadOrderProvider(ILoadOrderListingsProvider listingsProvider)
        {
            _listingsProvider = listingsProvider;
        }

        public IEnumerable<IModListingGetter> Get() => _listingsProvider.Get();
    }
}