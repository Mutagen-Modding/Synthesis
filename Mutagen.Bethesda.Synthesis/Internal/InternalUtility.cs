using Mutagen.Bethesda.Internals;
using Mutagen.Bethesda.Synthesis.CLI;
using Noggog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Mutagen.Bethesda.Synthesis.Internal
{
    public class Utility
    {
        public static GameCategory TypeToGameCategory<TMod>()
            where TMod : IModGetter
        {
            switch (typeof(TMod).Name)
            {
                case "ISkyrimMod":
                case "ISkyrimModGetter":
                    return GameCategory.Skyrim;
                case "IOblivionMod":
                case "IOblivionModGetter":
                    return GameCategory.Oblivion;
                default:
                    throw new ArgumentException($"Unknown game type for: {typeof(TMod).Name}");
            }
        }

        public static SynthesisState<TModSetter, TModGetter> ToState<TModSetter, TModGetter>(RunSynthesisMutagenPatcher settings, PatcherPreferences userPrefs, ModKey exportKey)
            where TModSetter : class, IContextMod<TModSetter, TModGetter>, TModGetter
            where TModGetter : class, IContextGetterMod<TModSetter, TModGetter>
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
            var loadOrderListing = GetLoadOrder(settings.GameRelease, settings.LoadOrderFilePath, settings.DataFolderPath, userPrefs)
                .ToExtendedList();
            var rawLoadOrder = loadOrderListing.Select(x => new LoadOrderListing(x.ModKey, x.Enabled)).ToExtendedList();

            // Trim past Synthesis.esp
            var synthIndex = loadOrderListing.IndexOf(exportKey, (listing, key) => listing.ModKey == key);
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

            // Create or import patch mod
            TModSetter patchMod;
            ILinkCache<TModSetter, TModGetter> cache;
            if (userPrefs.NoPatch)
            {
                // Pass null, even though it isn't normally
                patchMod = null!;

                TModGetter readOnlyPatchMod;
                if (settings.SourcePath == null)
                {
                    readOnlyPatchMod = ModInstantiator<TModGetter>.Activator(exportKey, settings.GameRelease);
                }
                else
                {
                    readOnlyPatchMod = ModInstantiator<TModGetter>.Importer(new ModPath(exportKey, settings.SourcePath), settings.GameRelease);
                }
                loadOrder.Add(new ModListing<TModGetter>(readOnlyPatchMod, enabled: true));
                rawLoadOrder.Add(new LoadOrderListing(readOnlyPatchMod.ModKey, enabled: true));
                cache = loadOrder.ToImmutableLinkCache<TModSetter, TModGetter>();
            }
            else
            {
                if (settings.SourcePath == null)
                {
                    patchMod = ModInstantiator<TModSetter>.Activator(exportKey, settings.GameRelease);
                }
                else
                {
                    patchMod = ModInstantiator<TModSetter>.Importer(new ModPath(exportKey, settings.SourcePath), settings.GameRelease);
                }
                cache = loadOrder.ToMutableLinkCache(patchMod);
                loadOrder.Add(new ModListing<TModGetter>(patchMod, enabled: true));
                rawLoadOrder.Add(new LoadOrderListing(patchMod.ModKey, enabled: true));
            }

            return new SynthesisState<TModSetter, TModGetter>(
                settings: settings,
                loadOrder: loadOrder,
                rawLoadOrder: rawLoadOrder,
                linkCache: cache,
                patchMod: patchMod,
                extraDataPath: settings.ExtraDataFolder == null ? string.Empty : Path.GetFullPath(settings.ExtraDataFolder),
                defaultDataPath: settings.DefaultDataFolderPath,
                cancellation: userPrefs.Cancel);
        }

        public static IPatcherState ToState(GameCategory category, RunSynthesisMutagenPatcher settings, PatcherPreferences userPrefs, ModKey exportKey)
        {
            var regis = category.ToModRegistration();
            var method = typeof(Utility).GetMethods()
                .Where(m => m.Name == nameof(ToState))
                .Where(m => m.ContainsGenericParameters)
                .First()
                .MakeGenericMethod(regis.SetterType, regis.GetterType);
            return (IPatcherState)method.Invoke(null, new object[]
            {
                settings, 
                userPrefs,
                exportKey,
            })!;
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

        public static IEnumerable<LoadOrderListing> GetLoadOrder(
            GameRelease release,
            string loadOrderFilePath,
            string dataFolderPath,
            PatcherPreferences? userPrefs = null)
        {
            // This call will impliticly get Creation Club entries, too, as the Synthesis systems should be merging
            // things into a singular load order file for consumption here
            var loadOrderListing =
                ImplicitListings.GetListings(release, dataFolderPath)
                    .Select(x => new LoadOrderListing(x, enabled: true));
            if (!loadOrderFilePath.IsNullOrWhitespace())
            {
                loadOrderListing = loadOrderListing.Concat(PluginListings.RawListingsFromPath(loadOrderFilePath, release));
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
