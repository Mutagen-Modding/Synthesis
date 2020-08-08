using Mutagen.Bethesda.Internals;
using Noggog;
using Synthesis.Bethesda;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Mutagen.Bethesda.Synthesis.Internal
{
    public class Utility
    {
        public static SynthesisState<TMod, TModGetter> ToState<TMod, TModGetter>(RunSynthesisPatcher settings, UserPreferences userPrefs)
            where TMod : class, IMod, TModGetter
            where TModGetter : class, IModGetter
        {
            var loadOrderListing = SynthesisPipeline.Instance.GetLoadOrder(settings, userPrefs)
                .ToExtendedList();
            var loadOrder = LoadOrder.Import<TModGetter>(
                settings.DataFolderPath,
                loadOrderListing,
                settings.GameRelease);
            var modKey = ModKey.FromNameAndExtension(Path.GetFileName(settings.OutputPath));
            TMod patchMod;
            ILinkCache cache;
            if (settings.SourcePath == null)
            {
                patchMod = ModInstantiator<TMod>.Activator(modKey, settings.GameRelease);
            }
            else
            {
                patchMod = ModInstantiator<TMod>.Importer(new ModPath(modKey, settings.SourcePath), settings.GameRelease);
            }
            cache = loadOrder.ToMutableLinkCache(patchMod);
            loadOrder.Add(new ModListing<TModGetter>(patchMod));
            return new SynthesisState<TMod, TModGetter>(settings, loadOrder, cache, patchMod);
        }
    }
}
