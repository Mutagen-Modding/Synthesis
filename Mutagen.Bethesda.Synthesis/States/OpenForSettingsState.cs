using System.Drawing;
using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Plugins.Records;
using Noggog;
using Synthesis.Bethesda.Commands;

namespace Mutagen.Bethesda.Synthesis.States;

public class OpenForSettingsState : IOpenForSettingsState
{
    /// <summary>
    /// Instructions given to the patcher from the Synthesis pipeline
    /// </summary>
    public OpenForSettings Settings { get; }

    /// <summary>
    /// Current Load Order 
    /// </summary>
    public ILoadOrderGetter<ILoadOrderListingGetter> LoadOrder { get; }

    IReadOnlyList<ILoadOrderListingGetter> IBaseRunState.RawLoadOrder => LoadOrder.ListedOrder.ToList();

    public FilePath LoadOrderFilePath => Settings.LoadOrderFilePath;

    public DirectoryPath DataFolderPath => Settings.DataFolderPath;

    public GameRelease GameRelease => ((IBaseRunArgs)Settings).GameRelease;

    public Rectangle RecommendedOpenLocation { get; }
    
    public DirectoryPath? ExtraSettingsDataPath => Settings.ExtraDataFolder;
    public DirectoryPath? InternalDataPath => Settings.InternalDataFolder;
    public DirectoryPath? DefaultSettingsDataPath => Settings.DefaultDataFolderPath;
    
    public OpenForSettingsState(
        OpenForSettings settings,
        ILoadOrderGetter<ILoadOrderListingGetter> loadOrder)
    {
        Settings = settings;
        LoadOrder = loadOrder;
        RecommendedOpenLocation = new Rectangle(
            x: settings.Left, 
            y: settings.Top,
            width: settings.Width,
            height: settings.Height);
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