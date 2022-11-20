using System.IO.Abstractions;
using System.Reactive;
using System.Reactive.Linq;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments.DI;
using Mutagen.Bethesda.Installs.DI;
using Mutagen.Bethesda.Plugins.Order.DI;
using Noggog;
using Noggog.Reactive;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Synthesis.Bethesda.Execution.Profile;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles.Plugins;

public interface IProfileOverridesVm
{
    string? DataPathOverride { get; set; }
    GetResponse<DirectoryPath> DataFolderResult { get; }
    GameInstallMode? InstallModeOverride { get; set; }
    GameInstallMode InstallMode { get; }
    FilePath PluginListingsPath { get; }
}

public class ProfileOverridesVm : ViewModel,
    IProfileOverridesVm, 
    IDataDirectoryProvider,
    IGameInstallModeContext,
    IPluginListingsPathContext
{
    private readonly IPluginListingsPathProvider _pluginPathProvider;
    
    public IFileSystem FileSystem { get; }

    [Reactive]
    public string? DataPathOverride { get; set; }

    private readonly ObservableAsPropertyHelper<GetResponse<DirectoryPath>> _dataFolderResult;
    public GetResponse<DirectoryPath> DataFolderResult => _dataFolderResult.Value;
    
    DirectoryPath IDataDirectoryProvider.Path =>  _dataFolderResult.Value.Value;

    private readonly ObservableAsPropertyHelper<FilePath> _pluginListingsPath;
    public FilePath PluginListingsPath => _pluginListingsPath.Value;

    [Reactive]
    public GameInstallMode? InstallModeOverride { get; set; }

    private readonly ObservableAsPropertyHelper<GameInstallMode> _installMode;
    public GameInstallMode InstallMode => _installMode.Value;

    FilePath IPluginListingsPathContext.Path => PluginListingsPath;
    
    public ProfileOverridesVm(
        ILogger logger,
        ISchedulerProvider schedulerProvider,
        IWatchDirectory watchDirectory,
        IFileSystem fileSystem,
        IDataDirectoryLookup dataDirLookup,
        IPluginListingsPathProvider pluginPathProvider,
        IGameInstallLookup gameInstallModeProvider,
        IProfileIdentifier ident)
    {
        _pluginPathProvider = pluginPathProvider;
        FileSystem = fileSystem;
        
        _installMode = this.WhenAnyValue(x => x.InstallModeOverride)
            .ObserveOn(RxApp.TaskpoolScheduler)
            .Select(x =>
            {
                if (x != null) return x.Value;
                var installs = gameInstallModeProvider.GetInstallMode(ident.Release);

                foreach (var mode in Enums<GameInstallMode>.Values)
                {
                    if (installs.HasFlag(mode)) return mode;
                }

                return default(GameInstallMode);
            })
            .ToProperty(this, nameof(InstallMode), GameInstallMode.Steam, scheduler: schedulerProvider.MainThread, deferSubscription: false);
        
        _dataFolderResult = this.WhenAnyValue(x => x.DataPathOverride)
            .Select(path =>
            {
                if (path != null) return Observable.Return(GetResponse<DirectoryPath>.Succeed(path));
                return this.WhenAnyValue(x => x.InstallMode)
                    .ObserveOn(schedulerProvider.TaskPool)
                    .Select(installMode =>
                    {
                        try
                        {
                            logger.Information("Starting to locate data folder for {Release} {InstallMode}", ident.Release, installMode);
                            if (!dataDirLookup.TryGet(ident.Release, installMode, out var dataFolder))
                            {
                                return GetResponse<DirectoryPath>.Fail(
                                    $"Could not automatically locate Data folder.  Run {installMode} once to properly register things.");
                            }

                            logger.Information("Found data folder at {DataFolder}", dataFolder);
                            return GetResponse<DirectoryPath>.Succeed(dataFolder);
                        }
                        catch (Exception ex)
                        {
                            return GetResponse<DirectoryPath>.Fail(string.Empty, ex);
                        }
                    });
            })
            .Switch()
            // Watch folder for existence
            .Select(x =>
            {
                if (x.Failed) return Observable.Return(x);
                return watchDirectory.Watch(x.Value)
                    .StartWith(Unit.Default)
                    .Select(_ =>
                    {
                        try
                        {
                            if (fileSystem.Directory.Exists(x.Value)) return x;
                            return GetResponse<DirectoryPath>.Fail(x.Value, $"Data folder did not exist: {x.Value}");
                        }
                        catch (Exception ex)
                        {
                            return GetResponse<DirectoryPath>.Fail(string.Empty, ex);
                        }
                    });
            })
            .Switch()
            .Do(d =>
            {
                if (d.Failed)
                {
                    logger.Error("Could not locate data folder: {Reason}", d.Reason);
                }
                else
                {
                    logger.Information("Data Folder: {DataFolderPath}", d.Value);
                }
            })
            .ToProperty(this, nameof(DataFolderResult), scheduler: schedulerProvider.MainThread, deferSubscription: true);
        
        _pluginListingsPath = this.WhenAnyValue(x => x.InstallMode)
            .Select(x => _pluginPathProvider.Get(ident.Release, x))
            .ToProperty(this, nameof(PluginListingsPath), scheduler: schedulerProvider.MainThread, deferSubscription: true);
    }
}