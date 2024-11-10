using Mutagen.Bethesda.Plugins.Order;

namespace Synthesis.Bethesda.Execution.Profile.Services;

public interface IProfileLoadOrderProvider
{
    IEnumerable<ILoadOrderListingGetter> Get();
}