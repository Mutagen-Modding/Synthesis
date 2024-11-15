using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Plugins.Order.DI;
using Synthesis.Bethesda.Execution.Profile.Services;

namespace Synthesis.Bethesda.CLI.RunPipeline;

public class ProfileLoadOrderProvider : IProfileLoadOrderProvider
{
    private readonly ILoadOrderListingsProvider _listingsProvider;

    public ProfileLoadOrderProvider(ILoadOrderListingsProvider listingsProvider)
    {
        _listingsProvider = listingsProvider;
    }

    public IEnumerable<ILoadOrderListingGetter> Get() => _listingsProvider.Get();
}