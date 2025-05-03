using Mutagen.Bethesda.Assets.DI;
using Mutagen.Bethesda.Plugins.Allocators;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Synthesis.CLI;
using Noggog;

namespace Mutagen.Bethesda.Synthesis;
// Note:  Keep named as-is, as there's patchers out there that reference this class directly that would break

/// <summary>
/// A class housing all the tools, parameters, and entry points for a typical Synthesis patcher
/// </summary>
[Obsolete("Use IPatcherState instead")]
public class SynthesisState<TModSetter, TModGetter> : IPatcherState<TModSetter, TModGetter>
    where TModSetter : class, IMod, TModGetter
    where TModGetter : class, IModGetter
{
    /// <inheritdoc />
    public ILoadOrder<IModListing<TModGetter>> LoadOrder { get; }

    /// <inheritdoc />
    public IReadOnlyList<ILoadOrderListingGetter> RawLoadOrder { get; }

    /// <inheritdoc />
    public ILinkCache<TModSetter, TModGetter> LinkCache { get; }

    /// <inheritdoc />
    public IAssetProvider AssetProvider { get; }

    /// <inheritdoc />
    public TModSetter PatchMod { get; }
    IMod IPatcherState.PatchMod => PatchMod;

    /// <inheritdoc />
    public CancellationToken Cancel { get; }

    public ILoadOrderGetter<IModListingGetter<IModFlagsGetter>> LoadOrderForPipeline => LoadOrder;
    
    ILinkCache IPatcherState.LinkCache => LinkCache;

    /// <inheritdoc />
    public DirectoryPath? ExtraSettingsDataPath { get; }

    /// <inheritdoc />
    public DirectoryPath? InternalDataPath { get; }

    /// <inheritdoc />
    public DirectoryPath? DefaultSettingsDataPath { get; }

    /// <inheritdoc />
    public FilePath LoadOrderFilePath { get; }

    /// <inheritdoc />
    public DirectoryPath DataFolderPath { get; }
    /// <inheritdoc />
    public GameRelease GameRelease { get; }

    /// <inheritdoc />
    public FilePath OutputPath { get; }

    /// <inheritdoc />
    public FilePath? SourcePath { get; }

    IFormKeyAllocator? IPatcherState.FormKeyAllocator => _formKeyAllocator;

    private readonly IFormKeyAllocator? _formKeyAllocator;

    public SynthesisState(
        RunSynthesisMutagenPatcher runArguments,
        IReadOnlyList<ILoadOrderListingGetter> rawLoadOrder,
        ILoadOrder<IModListing<TModGetter>> loadOrder,
        ILinkCache<TModSetter, TModGetter> linkCache,
        IAssetProvider assetProvider,
        TModSetter patchMod,
        DirectoryPath? extraDataPath,
        DirectoryPath? internalDataPath,
        DirectoryPath? defaultDataPath,
        CancellationToken cancellation,
        IFormKeyAllocator? formKeyAllocator)
    {
        LinkCache = linkCache;
        RawLoadOrder = rawLoadOrder;
        LoadOrder = loadOrder;
        PatchMod = patchMod;
        ExtraSettingsDataPath = extraDataPath;
        InternalDataPath = internalDataPath;
        DefaultSettingsDataPath = defaultDataPath;
        Cancel = cancellation;
        LoadOrderFilePath = runArguments.LoadOrderFilePath;
        DataFolderPath = runArguments.DataFolderPath;
        GameRelease = runArguments.GameRelease;
        OutputPath = runArguments.OutputPath;
        SourcePath = runArguments.SourcePath;
        _formKeyAllocator = formKeyAllocator;
        AssetProvider = assetProvider;
    }

    public void Dispose()
    {
        LoadOrder.Dispose();
        if (_formKeyAllocator is IDisposable disp)
        {
            disp.Dispose();
        }
    }
}