using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Plugins.Records;
using Noggog;
using Mutagen.Bethesda.Environments;
using Synthesis.Bethesda.Commands;

namespace Mutagen.Bethesda.Synthesis;

/// <summary>
/// A class housing all the tools, parameters, and entry points for a typical Synthesis check runnability analysis
/// </summary>
public class RunnabilityState : IRunnabilityState
{
    /// <summary>
    /// Instructions given to the patcher from the Synthesis pipeline
    /// </summary>
    public CheckRunnability Settings { get; }

    /// <summary>
    /// Current Load Order 
    /// </summary>
    public ILoadOrderGetter<ILoadOrderListingGetter> LoadOrder { get; }

    IReadOnlyList<ILoadOrderListingGetter> IBaseRunState.RawLoadOrder => LoadOrder.ListedOrder.ToList();
    public FilePath LoadOrderFilePath => Settings.LoadOrderFilePath;

    public DirectoryPath DataFolderPath => Settings.DataFolderPath;

    public GameRelease GameRelease => Settings.GameRelease;
    
    public DirectoryPath? ExtraSettingsDataPath => Settings.ExtraDataFolder;
    public DirectoryPath? InternalDataPath => Settings.InternalDataFolder;
    public DirectoryPath? DefaultSettingsDataPath => Settings.DefaultDataFolderPath;

    public RunnabilityState(
        CheckRunnability settings,
        ILoadOrderGetter<ILoadOrderListingGetter> loadOrder)
    {
        Settings = settings;
        LoadOrder = loadOrder;
    }

    public IGameEnvironment<TModSetter, TModGetter> GetEnvironmentState<TModSetter, TModGetter>()
        where TModSetter : class, IContextMod<TModSetter, TModGetter>, TModGetter
        where TModGetter : class, IContextGetterMod<TModSetter, TModGetter>
    {
        var lo = Plugins.Order.LoadOrder.Import<TModGetter>(DataFolderPath, LoadOrder.ListedOrder, GameRelease);
        return new GameEnvironmentState<TModSetter, TModGetter>(
            gameRelease: GameRelease,
            dataFolderPath: DataFolderPath,
            loadOrderFilePath: LoadOrderFilePath,
            creationClubListingsFilePath: null,
            loadOrder: lo,
            linkCache: lo.ToImmutableLinkCache<TModSetter, TModGetter>());
    }
}