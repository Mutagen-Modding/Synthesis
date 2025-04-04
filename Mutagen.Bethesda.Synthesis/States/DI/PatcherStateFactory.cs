using System.IO.Abstractions;
using Mutagen.Bethesda.Archives.DI;
using Mutagen.Bethesda.Assets.DI;
using Mutagen.Bethesda.Environments.DI;
using Mutagen.Bethesda.Inis.DI;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Allocators;
using Mutagen.Bethesda.Plugins.Binary.Headers;
using Mutagen.Bethesda.Plugins.Binary.Parameters;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins.Meta;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Plugins.Order.DI;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Strings;
using Mutagen.Bethesda.Strings.DI;
using Mutagen.Bethesda.Synthesis.CLI;
using Noggog;
using Synthesis.Bethesda;

namespace Mutagen.Bethesda.Synthesis.States.DI;

public interface IPatcherStateFactory
{
#pragma warning disable CS0618
    SynthesisState<TModSetter, TModGetter> ToState<TModSetter, TModGetter>(RunSynthesisMutagenPatcher settings, PatcherPreferences userPrefs, ModKey exportKey)
        where TModSetter : class, IContextMod<TModSetter, TModGetter>, TModGetter
        where TModGetter : class, IContextGetterMod<TModSetter, TModGetter>;
#pragma warning restore CS0618

    IPatcherState ToState(GameCategory category, RunSynthesisMutagenPatcher settings, PatcherPreferences userPrefs, ModKey exportKey);
}

public class PatcherStateFactory : IPatcherStateFactory
{
    private readonly IFileSystem _fileSystem;
    private readonly ILoadOrderImporterFactory _loadOrderImporter;
    private readonly IGetStateLoadOrder _getStateLoadOrder;

    public PatcherStateFactory(
        IFileSystem fileSystem,
        ILoadOrderImporterFactory loadOrderImporter,
        IGetStateLoadOrder getStateLoadOrder)
    {
        _fileSystem = fileSystem;
        _loadOrderImporter = loadOrderImporter;
        _getStateLoadOrder = getStateLoadOrder;
    }

    private class Utf8EncodingWrapper : IMutagenEncodingProvider
    {
        public IMutagenEncoding GetEncoding(GameRelease release, Language language)
        {
            return MutagenEncoding._utf8;
        }
    }
        
#pragma warning disable CS0618
    public SynthesisState<TModSetter, TModGetter> ToState<TModSetter, TModGetter>(RunSynthesisMutagenPatcher settings, PatcherPreferences userPrefs, ModKey exportKey)
        where TModSetter : class, IContextMod<TModSetter, TModGetter>, TModGetter
        where TModGetter : class, IContextGetterMod<TModSetter, TModGetter>
    {
#pragma warning restore CS0618
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
        var loadOrderListing = _getStateLoadOrder.GetFinalLoadOrder(
            gameRelease: settings.GameRelease,
            exportKey: exportKey,
            dataFolderPath: settings.DataFolderPath,
            addCcMods: !settings.LoadOrderIncludesCreationClub,
            userPrefs: userPrefs);

        var stringReadParams = new StringsReadParameters()
        {
            TargetLanguage = settings.TargetLanguage,
            EncodingProvider = settings.UseUtf8ForEmbeddedStrings ? new Utf8EncodingWrapper() : null
        };

        var loadOrder = _loadOrderImporter
            .Get<TModGetter>(
                settings.DataFolderPath,
                loadOrderListing.ProcessedLoadOrder,
                settings.GameRelease)
            .Import(new BinaryReadParameters()
            {
                StringsParam = stringReadParams,
                FileSystem = _fileSystem,
            });

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
                // ToDo
                // Eventually use generic read builder
                Cache<IModMasterStyledGetter, ModKey>? masterFlagsLookup = null;
                if (GameConstants.Get(settings.GameRelease).SeparateMasterLoadOrders)
                {
                    var header = ModHeaderFrame.FromPath(settings.SourcePath, settings.GameRelease, _fileSystem);
                    masterFlagsLookup = new Cache<IModMasterStyledGetter, ModKey>(x => x.ModKey);
                    foreach (var master in header.Masters(exportKey).Select(x => x.Master))
                    {
                        var otherPath = Path.Combine(settings.DataFolderPath, master.FileName);
                        var otherHeader = ModHeaderFrame.FromPath(otherPath, settings.GameRelease, _fileSystem);
                        masterFlagsLookup.Add(new KeyedMasterStyle(master, otherHeader.MasterStyle));
                    }
                }

                readOnlyPatchMod = ModInstantiator<TModGetter>.Importer(
                    new ModPath(exportKey, settings.SourcePath),
                    settings.GameRelease, 
                    new BinaryReadParameters()
                    {
                        FileSystem = _fileSystem,
                        StringsParam = stringReadParams,
                        MasterFlagsLookup = masterFlagsLookup
                    });
            }
            loadOrder.Add(new ModListing<TModGetter>(readOnlyPatchMod, enabled: true));
            loadOrderListing.Raw.Add(new LoadOrderListing(readOnlyPatchMod.ModKey, enabled: true));
            cache = loadOrder.ToImmutableLinkCache<TModSetter, TModGetter>();
        }
        else
        {
            if (settings.SourcePath == null)
            {
                Console.WriteLine("Creating new mod:");
                Console.WriteLine($"  ModKey: {exportKey}");
                Console.WriteLine($"  GameRelease: {settings.GameRelease}");
                if (settings.HeaderVersionOverride != null)
                {
                    Console.WriteLine($"  HeaderVersion: {settings.HeaderVersionOverride}");
                }

                var forceFormIdLowerRange = settings.FormIDRangeMode.ToForceBool();

                if (forceFormIdLowerRange != null)
                {
                    Console.WriteLine($"  Force FormID Lower Range: {forceFormIdLowerRange}");
                }
                patchMod = ModInstantiator<TModSetter>.Activator(
                    exportKey,
                    settings.GameRelease,
                    headerVersion: settings.HeaderVersionOverride,
                    forceUseLowerFormIDRanges: forceFormIdLowerRange);
                Console.WriteLine($"  Next FormID: {patchMod.NextFormID}");
            }
            else
            {
                patchMod = ModInstantiator<TModSetter>.Importer(
                    new ModPath(exportKey, settings.SourcePath), 
                    settings.GameRelease,
                    new BinaryReadParameters()
                    {
                        FileSystem = _fileSystem,
                        StringsParam = stringReadParams
                    });
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
            loadOrderListing.Raw.Add(new LoadOrderListing(patchMod.ModKey, enabled: true));

            System.Console.WriteLine($"Can use localization: {patchMod.CanUseLocalization}");
            if (patchMod.CanUseLocalization)
            {
                System.Console.WriteLine($"Localized: {settings.Localize}");
                patchMod.UsingLocalization = settings.Localize;
            }
        }

        var dataDir = new DataDirectoryInjection(settings.DataFolderPath);
        var rel = new GameReleaseInjection(settings.GameRelease);
        var archiveExt = new ArchiveExtensionProvider(rel);
        var loListings = new LoadOrderListingsInjection(
            loadOrderListing.ProcessedLoadOrder);
        var gameDirectoryLookup = new GameDirectoryLookupInjection(rel.Release, dataDir.Path.Directory);
        var assetProvider = new GameAssetProvider(
            new DataDirectoryAssetProvider(
                _fileSystem,
                dataDir),
            new ArchiveAssetProvider(
                _fileSystem,
                new GetApplicableArchivePaths(
                    _fileSystem,
                    new CheckArchiveApplicability(
                        archiveExt),
                    dataDir,
                    archiveExt,
                    new CachedArchiveListingDetailsProvider(
                        loListings,
                        new GetArchiveIniListings(
                            _fileSystem,
                            new IniPathProvider(
                                rel,
                                new IniPathLookup(
                                    gameDirectoryLookup))),
                        new ArchiveNameFromModKeyProvider(rel))),
                rel));

#pragma warning disable CS0618 // Type or member is obsolete
        return new SynthesisState<TModSetter, TModGetter>(
#pragma warning restore CS0618 // Type or member is obsolete
            runArguments: settings,
            loadOrder: loadOrder,
            assetProvider: assetProvider,
            rawLoadOrder: loadOrderListing.Raw,
            linkCache: cache,
            internalDataPath: settings.InternalDataFolder,
            patchMod: patchMod,
            extraDataPath: settings.ExtraDataFolder,
            defaultDataPath: settings.DefaultDataFolderPath,
            cancellation: userPrefs.Cancel,
            formKeyAllocator: formKeyAllocator);
    }

    public IPatcherState ToState(GameCategory category, RunSynthesisMutagenPatcher settings, PatcherPreferences userPrefs, ModKey exportKey)
    {
        var regis = category.ToModRegistration();
        var method = typeof(PatcherStateFactory)
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