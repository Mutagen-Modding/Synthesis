using System.Drawing;
using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Plugins.Order.DI;
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

    private IBaseRunArgs BaseRunArgs => Settings;

    /// <summary>
    /// Current Load Order 
    /// </summary>
    public ILoadOrderGetter<ILoadOrderListingGetter> LoadOrder { get; }

    IReadOnlyList<ILoadOrderListingGetter> IBaseRunState.RawLoadOrder => LoadOrder.ListedOrder.ToList();

    public FilePath LoadOrderFilePath => BaseRunArgs.LoadOrderFilePath;

    public DirectoryPath DataFolderPath => BaseRunArgs.DataFolderPath;

    public GameRelease GameRelease => BaseRunArgs.GameRelease;

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
        return GameEnvironmentBuilder<TModSetter, TModGetter>.Create(GameRelease)
            .WithLoadOrder(LoadOrder.ListedOrder.ToArray())
            .WithTargetDataFolder(DataFolderPath)
            .WithResolver(t =>
            {
                if (t == typeof(IPluginListingsPathContext) && Settings.LoadOrderFilePath != null) return new PluginListingsPathInjection(Settings.LoadOrderFilePath);
                if (t == typeof(ICreationClubListingsPathProvider)) return new CreationClubListingsPathInjection(null);
                return default;
            })
            .Build();
    }
}