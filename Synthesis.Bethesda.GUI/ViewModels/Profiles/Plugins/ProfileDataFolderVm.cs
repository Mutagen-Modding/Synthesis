using System;
using System.IO.Abstractions;
using System.Reactive;
using System.Reactive.Linq;
using Mutagen.Bethesda.Environments.DI;
using Noggog;
using Noggog.Reactive;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Synthesis.Bethesda.Execution.Profile;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles.Plugins;

public interface IProfileDataFolderVm
{
    string? DataPathOverride { get; set; }
    IObservable<GetResponse<DirectoryPath>> DataFolderResult { get; }
    DirectoryPath Path { get; }
}

public class ProfileDataFolderVm : ViewModel, IProfileDataFolderVm, IDataDirectoryProvider
{
    public IFileSystem FileSystem { get; }
    public IGameDirectoryLookup GameLocator { get; }

    [Reactive]
    public string? DataPathOverride { get; set; }

    public IObservable<GetResponse<DirectoryPath>> DataFolderResult { get; }

    private readonly ObservableAsPropertyHelper<DirectoryPath> _path;
    public DirectoryPath Path => _path.Value;

    public ProfileDataFolderVm(
        ILogger logger,
        ISchedulerProvider schedulerProvider,
        IWatchDirectory watchDirectory,
        IFileSystem fileSystem,
        IGameDirectoryLookup gameLocator,
        IProfileIdentifier ident)
    {
        FileSystem = fileSystem;
        GameLocator = gameLocator;
        DataFolderResult = this.WhenAnyValue(x => x.DataPathOverride)
            .Select(path =>
            {
                if (path != null) return Observable.Return(GetResponse<DirectoryPath>.Succeed(path));
                logger.Information("Starting to locate data folder");
                return Observable.Return(ident.Release)
                    .ObserveOn(schedulerProvider.TaskPool)
                    .Select(x =>
                    {
                        try
                        {
                            if (!gameLocator.TryGet(x, out var gameFolder))
                            {
                                return GetResponse<DirectoryPath>.Fail(
                                    "Could not automatically locate Data folder.  Run Steam/GoG/etc once to properly register things.");
                            }

                            return GetResponse<DirectoryPath>.Succeed(System.IO.Path.Combine(gameFolder, "Data"));
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
                            return GetResponse<DirectoryPath>.Fail($"Data folder did not exist: {x.Value}");
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
            .Replay(1).RefCount();

        _path = DataFolderResult
            .Select(x => x.Value)
            // Don't want another dispatch onto UI thread
            .ToProperty(this, nameof(Path), deferSubscription: true);
    }
}