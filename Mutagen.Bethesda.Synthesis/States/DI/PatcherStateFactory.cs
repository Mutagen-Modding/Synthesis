using System.IO.Abstractions;
using Mutagen.Bethesda.Archives.DI;
using Mutagen.Bethesda.Assets.DI;
using Mutagen.Bethesda.Environments.DI;
using Mutagen.Bethesda.Inis.DI;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Allocators;
using Mutagen.Bethesda.Plugins.Analysis;
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

    private void MergeLoadOrderForSplitFiles(
        RunSynthesisMutagenPatcher settings,
        GetStateLoadOrder.LoadOrderReturn loadOrderListing)
    {
        if (settings.SourcePath == null) return;

        // Get the ModKey from the source file name
        var sourceModKey = ModKey.FromFileName(Path.GetFileName(settings.SourcePath));
        var sourceModPath = new ModPath(sourceModKey, settings.SourcePath);

        if (!MultiModFileAnalysis.IsMultiModFile(sourceModPath, fileSystem: _fileSystem))
        {
            return;
        }

        var splitFiles = MultiModFileAnalysis.GetSplitModFiles(sourceModPath, fileSystem: _fileSystem);
        var splitModKeys = splitFiles
            .Select(f => ModKey.FromFileName(Path.GetFileName(f)))
            .ToHashSet();

        // Remove split file entries from processed load order
        // (don't insert merged entry - it will be added when patchMod is created)
        RemoveModKeysFromList(loadOrderListing.ProcessedLoadOrder, splitModKeys);

        // Replace split entries with merged entry in raw load order
        int? firstSplitIndex = null;
        for (int i = loadOrderListing.Raw.Count - 1; i >= 0; i--)
        {
            if (splitModKeys.Contains(loadOrderListing.Raw[i].ModKey))
            {
                if (firstSplitIndex == null || i < firstSplitIndex)
                {
                    firstSplitIndex = i;
                }
                loadOrderListing.Raw.RemoveAt(i);
            }
        }

        if (firstSplitIndex != null)
        {
            loadOrderListing.Raw.Insert(firstSplitIndex.Value, new LoadOrderListing(sourceModPath.ModKey, enabled: true));
        }

        System.Console.WriteLine($"Removed {splitModKeys.Count} split file entries from load order (merged into {sourceModPath.ModKey})");
    }

    private static void RemoveModKeysFromList<T>(IList<T> list, HashSet<ModKey> modKeysToRemove)
        where T : IModKeyed
    {
        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (modKeysToRemove.Contains(list[i].ModKey))
            {
                list.RemoveAt(i);
            }
        }
    }

    private TModSetter ImportAndMergeSplitFiles<TModSetter, TModGetter>(
        ModPath sourceModPath,
        ModKey exportKey,
        RunSynthesisMutagenPatcher settings,
        StringsReadParameters stringReadParams)
        where TModSetter : class, IContextMod<TModSetter, TModGetter>, TModGetter
        where TModGetter : class, IContextGetterMod<TModSetter, TModGetter>
    {
        var sourcePathModKey = ModKey.FromFileName(Path.GetFileName(sourceModPath.Path));
        var splitFiles = MultiModFileAnalysis.GetSplitModFiles(sourceModPath, fileSystem: _fileSystem);

        System.Console.WriteLine($"Detected split source files for {sourcePathModKey}");
        System.Console.WriteLine($"  Found {splitFiles.Count} split files");

        // Create a new mod to merge into
        var patchMod = ModFactory<TModSetter>.Activator(
            exportKey,
            settings.GameRelease,
            headerVersion: settings.HeaderVersionOverride,
            forceUseLowerFormIDRanges: settings.FormIDRangeMode.ToForceBool());

        // Read each split file and copy records into the merged mod
        foreach (var splitFile in splitFiles)
        {
            System.Console.WriteLine($"  Reading split file: {splitFile}");
            // Use the export mod key when reading, so records have correct FormKeys
            var splitMod = ModFactory<TModGetter>.Importer(
                new ModPath(exportKey, splitFile),
                settings.GameRelease,
                new BinaryReadParameters()
                {
                    FileSystem = _fileSystem,
                    StringsParam = stringReadParams
                });

            // Copy all records from split mod to merged mod
            foreach (var rec in splitMod.EnumerateMajorRecords())
            {
                var recRegis = rec.Registration;
                patchMod.GetTopLevelGroup(recRegis.GetterType).AddUntyped(rec.DeepCopy());
            }

            // Dispose if possible
            if (splitMod is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        return patchMod;
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

        if (settings.SplitIfMaxMastersExceeded)
        {
            MergeLoadOrderForSplitFiles(settings, loadOrderListing);
        }

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
                readOnlyPatchMod = ModFactory<TModGetter>.Activator(exportKey, settings.GameRelease);
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

                readOnlyPatchMod = ModFactory<TModGetter>.Importer(
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
                patchMod = ModFactory<TModSetter>.Activator(
                    exportKey,
                    settings.GameRelease,
                    headerVersion: settings.HeaderVersionOverride,
                    forceUseLowerFormIDRanges: forceFormIdLowerRange);
                Console.WriteLine($"  Next FormID: {patchMod.NextFormID}");
            }
            else
            {
                var modPath = new ModPath(exportKey, settings.SourcePath!);

                if (_fileSystem.File.Exists(modPath.Path))
                {
                    // Normal case - source file exists
                    patchMod = ModFactory<TModSetter>.Importer(
                        modPath,
                        settings.GameRelease,
                        new BinaryReadParameters()
                        {
                            FileSystem = _fileSystem,
                            StringsParam = stringReadParams
                        });
                }
                else
                {
                    // Check for split files only if enabled
                    if (settings.SplitIfMaxMastersExceeded)
                    {
                        var sourceModKey = ModKey.FromFileName(Path.GetFileName(settings.SourcePath!));
                        var sourceModPath = new ModPath(sourceModKey, settings.SourcePath!);

                        if (MultiModFileAnalysis.IsMultiModFile(sourceModPath, fileSystem: _fileSystem))
                        {
                            // Split files exist - read and merge them
                            patchMod = ImportAndMergeSplitFiles<TModSetter, TModGetter>(
                                sourceModPath,
                                exportKey,
                                settings,
                                stringReadParams);
                        }
                        else
                        {
                            throw new FileNotFoundException(modPath.Path);
                        }
                    }
                    else
                    {
                        throw new FileNotFoundException(modPath.Path);
                    }
                }
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