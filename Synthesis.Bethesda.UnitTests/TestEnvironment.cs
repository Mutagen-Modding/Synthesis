using System.Collections.Generic;
using System.IO.Abstractions;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins.Order;

namespace Synthesis.Bethesda.UnitTests
{
    public record TestEnvironment(
        IFileSystem FileSystem,
        GameRelease Release,
        string BaseFolder,
        string DataFolder,
        string PluginPath)
    {
        public IEnumerable<IModListingGetter> GetTypicalLoadOrder()
        {
            var listingsFromPath = new PluginListingsRetriever(FileSystem, new TimestampAligner(FileSystem));
            return listingsFromPath.ListingsFromPath(PluginPath, Release, DataFolder);
        }
    }
}