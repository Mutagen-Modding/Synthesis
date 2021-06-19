using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Order;
using Noggog;

namespace Mutagen.Bethesda.Synthesis.States
{
    public interface IGetStateLoadOrder
    {
        IEnumerable<IModListingGetter> GetLoadOrder(
            GameRelease release,
            string loadOrderFilePath,
            string dataFolderPath,
            PatcherPreferences? userPrefs = null);
    }

    public class GetStateLoadOrder : IGetStateLoadOrder
    {
        private readonly IFileSystem _FileSystem;
        private readonly IPluginListingsRetriever _PluginListingsRetriever;

        public GetStateLoadOrder(
            IFileSystem fileSystem,
            IPluginListingsRetriever pluginListingsRetriever)
        {
            _FileSystem = fileSystem;
            _PluginListingsRetriever = pluginListingsRetriever;
        }
        
        public IEnumerable<IModListingGetter> GetLoadOrder(
            GameRelease release,
            string loadOrderFilePath,
            string dataFolderPath,
            PatcherPreferences? userPrefs = null)
        {
            // This call will implicitly get Creation Club entries, too, as the Synthesis systems should be merging
            // things into a singular load order file for consumption here
            var loadOrderListing =
                Implicits.Get(release).Listings
                    .Where(x => _FileSystem.File.Exists(Path.Combine(dataFolderPath, x.FileName)))
                    .Select<ModKey, IModListingGetter>(x => new ModListing(x, enabled: true));
            if (!loadOrderFilePath.IsNullOrWhitespace())
            {
                loadOrderListing = loadOrderListing.Concat(_PluginListingsRetriever.RawListingsFromPath(loadOrderFilePath, release));
            }
            loadOrderListing = loadOrderListing.Distinct(x => x.ModKey);
            if (userPrefs?.InclusionMods != null)
            {
                var inclusions = userPrefs.InclusionMods.ToHashSet();
                loadOrderListing = loadOrderListing
                    .Where(m => inclusions.Contains(m.ModKey));
            }
            if (userPrefs?.ExclusionMods != null)
            {
                var exclusions = userPrefs.ExclusionMods.ToHashSet();
                loadOrderListing = loadOrderListing
                    .Where(m => !exclusions.Contains(m.ModKey));
            }
            return loadOrderListing;
        }
    }
}