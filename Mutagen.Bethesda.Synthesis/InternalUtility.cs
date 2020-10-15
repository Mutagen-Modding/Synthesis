using Mutagen.Bethesda.Synthesis.CLI;
using Noggog;
using System;
using System.IO;
using BaseSynthesis = Synthesis.Bethesda;

namespace Mutagen.Bethesda.Synthesis.Internal
{
    public class Utility
    {
        public static SynthesisState<TMod, TModGetter> ToState<TMod, TModGetter>(RunSynthesisMutagenPatcher settings, UserPreferences userPrefs)
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
            var synthIndex = loadOrderListing.IndexOf(BaseSynthesis.Constants.SynthesisModKey, (listing, key) => listing.ModKey == key);
            if (synthIndex != -1)
            {
                loadOrderListing.RemoveToCount(synthIndex);
            }
            if (!userPrefs.IncludeDisabledMods)
            {
                loadOrderListing = loadOrderListing.OnlyEnabled().ToExtendedList();
            }
            var loadOrder = LoadOrder.Import<TModGetter>(
                settings.DataFolderPath,
                loadOrderListing,
                settings.GameRelease);

            // Get Modkey from output path
            var modKey = BaseSynthesis.Constants.SynthesisModKey;

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
            loadOrder.Add(new ModListing<TModGetter>(patchMod, enabled: true));

            return new SynthesisState<TMod, TModGetter>(
                settings: settings,
                loadOrder: loadOrder,
                linkCache: cache,
                patchMod: patchMod,
                extraDataPath: Path.GetFullPath(settings.ExtraDataFolder) ?? string.Empty,
                cancellation: userPrefs.Cancel);
        }
    }
}
