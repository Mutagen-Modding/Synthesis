using Mutagen.Bethesda.Plugins.Order.DI;
using Noggog;

namespace Synthesis.Bethesda.IntegrationTests.Components;

public class PluginListingsPathInjection : IPluginListingsPathContext
{
    public FilePath Path { get; }

    public PluginListingsPathInjection(FilePath path)
    {
        Path = path;
    }
}
