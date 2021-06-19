using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Plugins.Utility;
using Mutagen.Bethesda.Synthesis.CLI;
using Noggog;

namespace Mutagen.Bethesda.Synthesis.States
{
    public interface IAddImplicitMasters
    {
        void Add(RunSynthesisMutagenPatcher settings, IList<IModListingGetter> loadOrderListing);
    }

    public class AddImplicitMasters : IAddImplicitMasters
    {
        private readonly IFileSystem _FileSystem;

        public AddImplicitMasters(IFileSystem fileSystem)
        {
            _FileSystem = fileSystem;
        }

        public void Add(RunSynthesisMutagenPatcher settings, IList<IModListingGetter> loadOrderListing)
        {
            HashSet<ModKey> referencedMasters = new();
            foreach (var item in loadOrderListing.OnlyEnabled())
            {
                MasterReferenceReader reader = MasterReferenceReader.FromPath(Path.Combine(settings.DataFolderPath, item.ModKey.FileName), settings.GameRelease, _FileSystem);
                referencedMasters.Add(reader.Masters.Select(m => m.Master));
            }
            for (int i = 0; i < loadOrderListing.Count; i++)
            {
                var listing = loadOrderListing[i];
                if (!listing.Enabled && referencedMasters.Contains(listing.ModKey))
                {
                    loadOrderListing[i] = new ModListing(listing.ModKey, enabled: true);
                }
            }
        }
    }
}