using Mutagen.Bethesda.Internals;
using Mutagen.Bethesda.Synthesis.CLI;
using Noggog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BaseSynthesis = Synthesis.Bethesda;

namespace Mutagen.Bethesda.Synthesis.Internal
{
    public class Utility
    {
        public static SynthesisState<TModSetter, TModGetter> ToState<TModSetter, TModGetter>(RunSynthesisMutagenPatcher settings, UserPreferences userPrefs)
            where TModSetter : class, IContextMod<TModSetter>, TModGetter
            where TModGetter : class, IContextGetterMod<TModSetter>
        {
            // Confirm target game release matches
            var regis = settings.GameRelease.ToCategory().ToModRegistration();
            if (!typeof(TModSetter).IsAssignableFrom(regis.SetterType))
            {
                throw new ArgumentException($"Target mod type {typeof(TModSetter)} was not of the expected type {regis.SetterType}");
            }
            if (!typeof(TModGetter).IsAssignableFrom(regis.GetterType))
            {
                throw new ArgumentException($"Target mod type {typeof(TModGetter)} was not of the expected type {regis.GetterType}");
            }

            // Get load order
            var loadOrderListing = SynthesisPipeline.Instance.GetLoadOrder(settings, userPrefs)
                .ToExtendedList();
            var rawLoadOrder = loadOrderListing.Select(x => new LoadOrderListing(x.ModKey, x.Enabled)).ToExtendedList();

            // Trim past Synthesis.esp
            var synthIndex = loadOrderListing.IndexOf(BaseSynthesis.Constants.SynthesisModKey, (listing, key) => listing.ModKey == key);
            if (synthIndex != -1)
            {
                loadOrderListing.RemoveToCount(synthIndex);
            }

            if (userPrefs.AddImplicitMasters)
            {
                AddImplicitMasters(settings, loadOrderListing);
            }

            // Remove disabled mods
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
            TModSetter patchMod;
            ILinkCache cache;
            if (userPrefs.NoPatch)
            {
                // Pass null, even though it isn't normally
                patchMod = null!;

                TModGetter readOnlyPatchMod;
                if (settings.SourcePath == null)
                {
                    readOnlyPatchMod = ModInstantiator<TModGetter>.Activator(modKey, settings.GameRelease);
                }
                else
                {
                    readOnlyPatchMod = ModInstantiator<TModGetter>.Importer(new ModPath(modKey, settings.SourcePath), settings.GameRelease);
                }
                loadOrder.Add(new ModListing<TModGetter>(readOnlyPatchMod, enabled: true));
                cache = loadOrder.ToImmutableLinkCache<TModSetter, TModGetter>();
            }
            else
            {
                if (settings.SourcePath == null)
                {
                    patchMod = ModInstantiator<TModSetter>.Activator(modKey, settings.GameRelease);
                }
                else
                {
                    patchMod = ModInstantiator<TModSetter>.Importer(new ModPath(modKey, settings.SourcePath), settings.GameRelease);
                }
                cache = loadOrder.ToMutableLinkCache(patchMod);
                loadOrder.Add(new ModListing<TModGetter>(patchMod, enabled: true));
            }

            return new SynthesisState<TModSetter, TModGetter>(
                settings: settings,
                loadOrder: loadOrder,
                rawLoadOrder: rawLoadOrder,
                linkCache: cache,
                patchMod: patchMod,
                extraDataPath: settings.ExtraDataFolder == null ? string.Empty : Path.GetFullPath(settings.ExtraDataFolder),
                cancellation: userPrefs.Cancel);
        }

        public static void AddImplicitMasters(RunSynthesisMutagenPatcher settings, ExtendedList<LoadOrderListing> loadOrderListing)
        {
            HashSet<ModKey> referencedMasters = new HashSet<ModKey>();
            foreach (var item in loadOrderListing.OnlyEnabled())
            {
                MasterReferenceReader reader = MasterReferenceReader.FromPath(Path.Combine(settings.DataFolderPath, item.ModKey.FileName), settings.GameRelease);
                referencedMasters.Add(reader.Masters.Select(m => m.Master));
            }
            for (int i = 0; i < loadOrderListing.Count; i++)
            {
                var listing = loadOrderListing[i];
                if (!listing.Enabled && referencedMasters.Contains(listing.ModKey))
                {
                    loadOrderListing[i] = new LoadOrderListing(listing.ModKey, enabled: true);
                }
            }
        }
    }
}
