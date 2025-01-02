using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Plugins.Records;
using Noggog;
using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Plugins.Order.DI;
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
        return GameEnvironmentBuilder<TModSetter, TModGetter>.Create(GameRelease)
            .WithLoadOrder(LoadOrder.ListedOrder.ToArray())
            .WithTargetDataFolder(DataFolderPath)
            .WithResolver(t =>
            {
                if (t == typeof(IPluginListingsPathContext)) return new PluginListingsPathInjection(Settings.LoadOrderFilePath);
                if (t == typeof(ICreationClubListingsPathProvider)) return new CreationClubListingsPathInjection(null);
                return default;
            })
            .Build();
    }
}