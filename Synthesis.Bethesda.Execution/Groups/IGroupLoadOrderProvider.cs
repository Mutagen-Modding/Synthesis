using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Order;
using Synthesis.Bethesda.Execution.Profile;
using Synthesis.Bethesda.Execution.Profile.Services;

namespace Synthesis.Bethesda.Execution.Groups;

public interface IGroupLoadOrderProvider
{
    IEnumerable<ILoadOrderListingGetter> Get(IReadOnlySet<ModKey> blacklisted);
}

public class GroupLoadOrderProvider : IGroupLoadOrderProvider
{
    private readonly IProfileLoadOrderProvider _profileLoadOrderProvider;

    public GroupLoadOrderProvider(
        IProfileLoadOrderProvider profileLoadOrderProvider)
    {
        _profileLoadOrderProvider = profileLoadOrderProvider;
    }
        
    public IEnumerable<ILoadOrderListingGetter> Get(IReadOnlySet<ModKey> blacklisted)
    {
        return _profileLoadOrderProvider.Get()
            .Where(x => !blacklisted.Contains(x.ModKey));
    }
}