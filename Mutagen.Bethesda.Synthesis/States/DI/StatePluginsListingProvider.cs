using System.Collections.Generic;
using System.Linq;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Plugins.Order.DI;
using Noggog;

namespace Mutagen.Bethesda.Synthesis.States.DI
{
    public class StatePluginsListingProvider : IPluginListingsProvider
    {
        private readonly string _PluginPath;
        private readonly IPluginRawListingsReader _Reader;

        public StatePluginsListingProvider(
            string pluginPath,
            IPluginRawListingsReader reader)
        {
            _PluginPath = pluginPath;
            _Reader = reader;
        }
        
        public IEnumerable<IModListingGetter> Get()
        {
            if (_PluginPath.IsNullOrWhitespace()) return Enumerable.Empty<IModListingGetter>();
            return _Reader.Read(_PluginPath);
        }
    }
}