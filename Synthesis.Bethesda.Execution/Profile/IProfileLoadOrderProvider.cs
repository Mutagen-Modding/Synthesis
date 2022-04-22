using Mutagen.Bethesda.Plugins.Order;

namespace Synthesis.Bethesda.Execution.Profile;

public interface IProfileLoadOrderProvider
{
    IEnumerable<IModListingGetter> Get();
}