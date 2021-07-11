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

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles.Plugins
{
    public interface IProfileDataFolder
    {
        string? DataPathOverride { get; set; }
        GetResponse<DirectoryPath> DataFolderResult { get; }
        DirectoryPath Path { get; }
    }

    public class ProfileDataFolder : ViewModel, IProfileDataFolder, IDataDirectoryProvider
    {
        [Reactive]
        public string? DataPathOverride { get; set; }

        private readonly ObservableAsPropertyHelper<GetResponse<DirectoryPath>> _DataFolderResult;
        public GetResponse<DirectoryPath> DataFolderResult => _DataFolderResult.Value;

        private readonly ObservableAsPropertyHelper<DirectoryPath> _Path;
        public DirectoryPath Path => _Path.Value;

        public ProfileDataFolder(
            ILogger logger,
            ISchedulerProvider schedulerProvider,
            IWatchDirectory watchDirectory,
            IFileSystem fileSystem,
            IGameDirectoryLookup gameLocator,
            IProfileIdentifier ident)
        {
            _DataFolderResult = this.WhenAnyValue(x => x.DataPathOverride)
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
                .StartWith(GetResponse<DirectoryPath>.Fail("Data folder uninitialized"))
                .Do(d =>
                {
                    if (d.Failed)
                    {
                        logger.Error($"Could not locate data folder: {d.Reason}");
                    }
                    else
                    {
                        logger.Information($"Data Folder: {d.Value}");
                    }
                })
                .ToGuiProperty(this, nameof(DataFolderResult));

            _Path = this.WhenAnyValue(x => x.DataFolderResult)
                .Select(x => x.Value)
                .ToGuiProperty(this, nameof(Path));
        }
    }
}