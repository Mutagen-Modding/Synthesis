using System.Collections.Generic;
using System.Linq;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Order;
using Synthesis.Bethesda.Execution.Profile;

namespace Synthesis.Bethesda.Execution.Groups;

public interface IGroupLoadOrderProvider
{
    IEnumerable<IModListingGetter> Get(IReadOnlySet<ModKey> blacklisted);
}

public class GroupLoadOrderProvider : IGroupLoadOrderProvider
{
    private readonly IProfileLoadOrderProvider _profileLoadOrderProvider;

    public GroupLoadOrderProvider(
        IProfileLoadOrderProvider profileLoadOrderProvider)
    {
        _profileLoadOrderProvider = profileLoadOrderProvider;
    }
        
    public IEnumerable<IModListingGetter> Get(IReadOnlySet<ModKey> blacklisted)
    {
        return _profileLoadOrderProvider.Get()
            .Where(x => !blacklisted.Contains(x.ModKey));
    }
}