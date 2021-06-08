using System;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using Mutagen.Bethesda.Installs;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

namespace Synthesis.Bethesda.GUI.Temporary
{
    public class ProfileDataFolder : ViewModel
    {
        [Reactive]
        public string? DataPathOverride { get; set; }
        
        public IObservable<GetResponse<string>> DataFolderResult { get; }

        private readonly ObservableAsPropertyHelper<string> _DataFolder;
        public string DataFolder => _DataFolder.Value;

        public ProfileDataFolder(
            ILogger logger,
            ProfileIdentifier ident)
        {
            DataFolderResult = this.WhenAnyValue(x => x.DataPathOverride)
                .Select(path =>
                {
                    if (path != null) return Observable.Return(GetResponse<string>.Succeed(path));
                    logger.Information("Starting to locate data folder");
                    return Observable.Return(ident.Release)
                        .ObserveOn(RxApp.TaskpoolScheduler)
                        .Select(x =>
                        {
                            try
                            {
                                if (!GameLocations.TryGetGameFolder(x, out var gameFolder))
                                {
                                    return GetResponse<string>.Fail("Could not automatically locate Data folder.  Run Steam/GoG/etc once to properly register things.");
                                }
                                return GetResponse<string>.Succeed(Path.Combine(gameFolder, "Data"));
                            }
                            catch (Exception ex)
                            {
                                return GetResponse<string>.Fail(string.Empty, ex);
                            }
                        });
                })
                .Switch()
                // Watch folder for existence
                .Select(x =>
                {
                    if (x.Failed) return Observable.Return(x);
                    return Noggog.ObservableExt.WatchFile(x.Value)
                        .StartWith(Unit.Default)
                        .Select(_ =>
                        {
                            if (Directory.Exists(x.Value)) return x;
                            return GetResponse<string>.Fail($"Data folder did not exist: {x.Value}");
                        });
                })
                .Switch()
                .StartWith(GetResponse<string>.Fail("Data folder uninitialized"))
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
                .Replay(1)
                .RefCount();

            _DataFolder = DataFolderResult
                .Select(x => x.Value)
                .ToGuiProperty<string>(this, nameof(DataFolder), string.Empty);
        }
    }
}