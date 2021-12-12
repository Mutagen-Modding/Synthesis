using System;
using Noggog;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Allocators;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Plugins.Records.Internals;
using Mutagen.Bethesda.Strings;
using Mutagen.Bethesda.Synthesis.CLI;

namespace Mutagen.Bethesda.Synthesis.States.DI
{
    public interface IStateFactory
    {
        SynthesisState<TModSetter, TModGetter> ToState<TModSetter, TModGetter>(RunSynthesisMutagenPatcher settings, PatcherPreferences userPrefs, ModKey exportKey)
            where TModSetter : class, IContextMod<TModSetter, TModGetter>, TModGetter
            where TModGetter : class, IContextGetterMod<TModSetter, TModGetter>;

        IPatcherState ToState(GameCategory category, RunSynthesisMutagenPatcher settings, PatcherPreferences userPrefs, ModKey exportKey);
    }

    public class StateFactory : IStateFactory
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILoadOrderImporterFactory _loadOrderImporter;
        private readonly IGetStateLoadOrder _getStateLoadOrder;
        private readonly IEnableImplicitMastersFactory _enableImplicitMasters;

        public StateFactory(
            IFileSystem fileSystem,
            ILoadOrderImporterFactory loadOrderImporter,
            IGetStateLoadOrder getStateLoadOrder,
            IEnableImplicitMastersFactory enableImplicitMasters)
        {
            _fileSystem = fileSystem;
            _loadOrderImporter = loadOrderImporter;
            _getStateLoadOrder = getStateLoadOrder;
            _enableImplicitMasters = enableImplicitMasters;
        }
        
        public SynthesisState<TModSetter, TModGetter> ToState<TModSetter, TModGetter>(RunSynthesisMutagenPatcher settings, PatcherPreferences userPrefs, ModKey exportKey)
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
            
            // Set up target language
            System.Console.WriteLine($"Language: {settings.TargetLanguage}");
            TranslatedString.DefaultLanguage = settings.TargetLanguage;

            // Get load order
            var loadOrderListing = _getStateLoadOrder.GetLoadOrder(!settings.LoadOrderIncludesCreationClub, userPrefs)
                .ToExtendedList();
            var rawLoadOrder = loadOrderListing.Select(x => new ModListing(x.ModKey, x.Enabled)).ToExtendedList();

            // Trim past export key
            var synthIndex = loadOrderListing.IndexOf(exportKey, (listing, key) => listing.ModKey == key);
            if (synthIndex != -1)
            {
                loadOrderListing.RemoveToCount(synthIndex);
            }

            if (userPrefs.AddImplicitMasters)
            {
                _enableImplicitMasters
                    .Get(settings.DataFolderPath, settings.GameRelease)
                    .Add(loadOrderListing);
            }

            // Remove disabled mods
            if (!userPrefs.IncludeDisabledMods)
            {
                loadOrderListing = loadOrderListing.OnlyEnabled().ToExtendedList();
            }

            var loadOrder = _loadOrderImporter
                .Get<TModGetter>(
                    settings.DataFolderPath,
                    loadOrderListing,
                    settings.GameRelease)
                .Import();

            // Create or import patch mod
            TModSetter patchMod;
            ILinkCache<TModSetter, TModGetter> cache;
            IFormKeyAllocator? formKeyAllocator = null;
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
                    readOnlyPatchMod = ModInstantiator<TModGetter>.Importer(new ModPath(exportKey, settings.SourcePath), settings.GameRelease, fileSystem: _fileSystem);
                }
                loadOrder.Add(new ModListing<TModGetter>(readOnlyPatchMod, enabled: true));
                rawLoadOrder.Add(new ModListing(readOnlyPatchMod.ModKey, enabled: true));
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
                    patchMod = ModInstantiator<TModSetter>.Importer(new ModPath(exportKey, settings.SourcePath), settings.GameRelease, fileSystem: _fileSystem);
                }
                if (settings.PersistencePath is not null && settings.PatcherName is not null)
                {
                    if (TextFileSharedFormKeyAllocator.IsPathOfAllocatorType(settings.PersistencePath))
                    {
                        System.Console.WriteLine($"Using {nameof(TextFileSharedFormKeyAllocator)} allocator");
                        patchMod.SetAllocator(formKeyAllocator = new TextFileSharedFormKeyAllocator(patchMod, settings.PersistencePath, settings.PatcherName, fileSystem: _fileSystem));
                    }
                    // else if (SQLiteFormKeyAllocator.IsPathOfAllocatorType(settings.PersistencePath))
                    // {
                    //     System.Console.WriteLine($"Using {nameof(SQLiteFormKeyAllocator)} allocator");
                    //     patchMod.SetAllocator(formKeyAllocator = new SQLiteFormKeyAllocator(patchMod, settings.PersistencePath, settings.PatcherName));
                    // }
                    else
                    {
                        System.Console.WriteLine($"Allocation systems were marked to be on, but could not identify allocation system to be used");
                    }
                }
                cache = loadOrder.ToMutableLinkCache(patchMod);
                loadOrder.Add(new ModListing<TModGetter>(patchMod, enabled: true));
                rawLoadOrder.Add(new ModListing(patchMod.ModKey, enabled: true));

                if (patchMod.CanUseLocalization)
                {
                    patchMod.UsingLocalization = true;
                }
            }
            
            System.Console.WriteLine($"Can use localization: {patchMod.CanUseLocalization}");
            if (patchMod.CanUseLocalization)
            {
                System.Console.WriteLine($"Localized: {settings.Localize}");
                patchMod.UsingLocalization = settings.Localize;
            }

            return new SynthesisState<TModSetter, TModGetter>(
                runArguments: settings,
                loadOrder: loadOrder,
                rawLoadOrder: rawLoadOrder,
                linkCache: cache,
                internalDataPath: settings.InternalDataFolder,
                patchMod: patchMod,
                extraDataPath: settings.ExtraDataFolder == null ? string.Empty : Path.GetFullPath(settings.ExtraDataFolder),
                defaultDataPath: settings.DefaultDataFolderPath,
                cancellation: userPrefs.Cancel,
                formKeyAllocator: formKeyAllocator);
        }

        public IPatcherState ToState(GameCategory category, RunSynthesisMutagenPatcher settings, PatcherPreferences userPrefs, ModKey exportKey)
        {
            var regis = category.ToModRegistration();
            var method = typeof(StateFactory)
                .GetMethods()
                .Where(m => m.Name == nameof(ToState))
                .Where(m => m.ContainsGenericParameters)
                .First()
                .MakeGenericMethod(regis.SetterType, regis.GetterType);
            return (IPatcherState)method.Invoke(this, new object[]
            {
                settings, 
                userPrefs,
                exportKey
            })!;
        }
    }
}