using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Plugins.Order.DI;
using Noggog;

namespace Mutagen.Bethesda.Synthesis.States.DI;

public class StatePluginsListingProvider : IPluginListingsProvider
{
    private readonly string _pluginPath;
    private readonly IPluginRawListingsReader _reader;

    public StatePluginsListingProvider(
        string pluginPath,
        IPluginRawListingsReader reader)
    {
        _pluginPath = pluginPath;
        _reader = reader;
    }
        
    public IEnumerable<ILoadOrderListingGetter> Get()
    {
        if (_pluginPath.IsNullOrWhitespace()) return Enumerable.Empty<IModListingGetter>();
        return _reader.Read(_pluginPath);
    }
}