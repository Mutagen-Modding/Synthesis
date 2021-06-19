using System.Collections.Generic;
using System.Linq;
using Mutagen.Bethesda.Core.Plugins.Order;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Synthesis.CLI;

namespace Mutagen.Bethesda.Synthesis.States
{
    public interface IEnableImplicitMasters
    {
        void Add(RunSynthesisMutagenPatcher settings, IList<IModListingGetter> loadOrderListing);
    }

    public class EnableImplicitMasters : IEnableImplicitMasters
    {
        private readonly IFindImplicitlyIncludedMods _FindImplicitlyIncludedMods;

        public EnableImplicitMasters(
            IFindImplicitlyIncludedMods findImplicitlyIncludedMods)
        {
            _FindImplicitlyIncludedMods = findImplicitlyIncludedMods;
        }

        public void Add(RunSynthesisMutagenPatcher settings, IList<IModListingGetter> loadOrderListing)
        {
            var implicitlyAdded = _FindImplicitlyIncludedMods.Find(settings.GameRelease, settings.DataFolderPath, loadOrderListing)
                .ToHashSet();
            for (int i = loadOrderListing.Count - 1; i >= 0; i--)
            {
                var listing = loadOrderListing[i];
                if (!listing.Enabled && implicitlyAdded.Contains(listing.ModKey))
                {
                    loadOrderListing[i] = new ModListing(listing.ModKey, enabled: true);
                }
            }
        }
    }
}