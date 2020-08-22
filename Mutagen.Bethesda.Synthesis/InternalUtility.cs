using Mutagen.Bethesda.Internals;
using Noggog;
using Synthesis.Bethesda;
using System;
using Constants = Synthesis.Bethesda.Constants;

namespace Mutagen.Bethesda.Synthesis.Internal
{
    public class Utility
    {
        public static SynthesisState<TMod, TModGetter> ToState<TMod, TModGetter>(RunSynthesisPatcher settings, UserPreferences userPrefs)
            where TMod : class, IMod, TModGetter
            where TModGetter : class, IModGetter
        {
            // Confirm target game release matches
            var regis = settings.GameRelease.ToCategory().ToModRegistration();
            if (!typeof(TMod).IsAssignableFrom(regis.SetterType))
            {
                throw new ArgumentException($"Target mod type {typeof(TMod)} was not of the expected type {regis.SetterType}");
            }
            if (!typeof(TModGetter).IsAssignableFrom(regis.GetterType))
            {
                throw new ArgumentException($"Target mod type {typeof(TModGetter)} was not of the expected type {regis.GetterType}");
            }

            // Get load order
            var loadOrderListing = SynthesisPipeline.Instance.GetLoadOrder(settings, userPrefs)
                .ToExtendedList();
            var synthIndex = loadOrderListing.IndexOf(Constants.SynthesisModKey, (listing, key) => listing.ModKey == key);
            if (synthIndex != -1)
            {
                loadOrderListing.RemoveToCount(synthIndex);
            }
            var loadOrder = LoadOrder.Import<TModGetter>(
                settings.DataFolderPath,
                loadOrderListing.OnlyEnabled(),
                settings.GameRelease);

            // Get Modkey from output path
            var modKey = Constants.SynthesisModKey;

            // Create or import patch mod
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

            // Create cache and loadorder for end use
            cache = loadOrder.ToMutableLinkCache(patchMod);
            loadOrder.Add(new ModListing<TModGetter>(patchMod));
            
            return new SynthesisState<TMod, TModGetter>(settings, loadOrder, cache, patchMod, userPrefs.Cancel);
        }
    }
}
