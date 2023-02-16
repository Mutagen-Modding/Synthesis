using System.IO.Abstractions;
using System.Reactive;
using System.Reactive.Linq;
using Mutagen.Bethesda.Environments.DI;
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
}

public class ProfileOverridesVm : ViewModel,
    IProfileOverridesVm, 
    IDataDirectoryProvider,
    IGameDirectoryProvider
{
    public IFileSystem FileSystem { get; }

    [Reactive]
    public string? DataPathOverride { get; set; }

    private readonly ObservableAsPropertyHelper<GetResponse<DirectoryPath>> _dataFolderResult;
    public GetResponse<DirectoryPath> DataFolderResult => _dataFolderResult.Value;
    
    DirectoryPath IDataDirectoryProvider.Path => _dataFolderResult.Value.Value;

    DirectoryPath? IGameDirectoryProvider.Path => DataFolderResult.Failed ? null : DataFolderResult.Value.Directory;
    
    public ProfileOverridesVm(
        ILogger logger,
        ISchedulerProvider schedulerProvider,
        IWatchDirectory watchDirectory,
        IFileSystem fileSystem,
        IDataDirectoryLookup dataDirLookup,
        IProfileIdentifier ident)
    {
        FileSystem = fileSystem;
        
        _dataFolderResult = this.WhenAnyValue(x => x.DataPathOverride)
            .Select(path =>
            {
                if (path != null) return Observable.Return(GetResponse<DirectoryPath>.Succeed(path));
                return Observable.Return(ident.Release)
                    .ObserveOn(schedulerProvider.TaskPool)
                    .Select(release =>
                    {
                        try
                        {
                            logger.Information("Starting to locate data folder for {Release}", release);
                            if (!dataDirLookup.TryGet(release, out var dataFolder))
                            {
                                return GetResponse<DirectoryPath>.Fail(
                                    $"Could not automatically locate Data folder.  Run game once to properly register things.");
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
    }
}