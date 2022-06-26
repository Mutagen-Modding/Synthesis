using System.IO.Abstractions;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using Mutagen.Bethesda.Plugins;
using Newtonsoft.Json;
using Noggog;
using Noggog.Reactive;
using ReactiveUI;
using Serilog;
using Synthesis.Bethesda.DTO;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel;

namespace Synthesis.Bethesda.GUI.Services.Patchers.Solution;

public interface ISolutionMetaFileSync
{
    IDisposable Sync();
}

public class SolutionMetaFileSync : ISolutionMetaFileSync
{
    private readonly IFileSystem _fileSystem;
    private readonly ILogger _logger;
    private readonly ISchedulerProvider _schedulerProvider;
    private readonly IPatcherNameVm _nameVm;
    private readonly ISolutionPatcherSettingsVm _patcherSettingsVm;
        
    public IObservable<string> MetaPath { get; }

    public SolutionMetaFileSync(
        IFileSystem fileSystem,
        ILogger logger,
        ISchedulerProvider schedulerProvider,
        IPatcherNameVm nameVm,
        ISelectedProjectInputVm selectedProjectInput,
        ISolutionPatcherSettingsVm patcherSettingsVm)
    {
        _fileSystem = fileSystem;
        _logger = logger;
        _schedulerProvider = schedulerProvider;
        _nameVm = nameVm;
        _patcherSettingsVm = patcherSettingsVm;

        MetaPath = selectedProjectInput.WhenAnyValue(x => x.Picker.TargetPath)
            .Select(projPath =>
            {
                try
                {
                    return System.IO.Path.Combine(System.IO.Path.GetDirectoryName(projPath)!, Constants.MetaFileName);
                }
                catch (Exception)
                {
                    return string.Empty;
                }
            })
            .Replay(1)
            .RefCount();
    }

    public IDisposable Sync()
    {
        var disp = new CompositeDisposable();
        MetaPath
            .Select(path =>
            {
                return Noggog.ObservableExt.WatchFile(path)
                    .Throttle(TimeSpan.FromMilliseconds(500), RxApp.MainThreadScheduler)
                    .StartWith(Unit.Default)
                    .Select(_ =>
                    {
                        if (!_fileSystem.File.Exists(path)) return default;
                        try
                        {
                            return JsonConvert.DeserializeObject<PatcherCustomization>(
                                _fileSystem.File.ReadAllText(path),
                                Execution.Constants.JsonSettings);
                        }
                        catch (Exception ex)
                        {
                            _logger.Error(ex, "Error reading in meta");
                        }
                        return default(PatcherCustomization?);
                    });
            })
            .Switch()
            .DistinctUntilChanged()
            .ObserveOn(_schedulerProvider.MainThread)
            .Subscribe(info =>
            {
                if (info == null) return;
                if (info.Nickname != null)
                {
                    _nameVm.Nickname = info.Nickname;
                }
                _patcherSettingsVm.LongDescription = info.LongDescription ?? string.Empty;
                _patcherSettingsVm.ShortDescription = info.OneLineDescription ?? string.Empty;
                _patcherSettingsVm.Visibility = info.Visibility;
                _patcherSettingsVm.Versioning = info.PreferredAutoVersioning;
                _patcherSettingsVm.SetRequiredMods(info.RequiredMods
                    .SelectWhere(x => TryGet<ModKey>.Create(ModKey.TryFromNameAndExtension(x, out var modKey), modKey)));
            })
            .DisposeWith(disp);
        
        Observable.CombineLatest(
                _nameVm.WhenAnyValue(x => x.Nickname),
                _patcherSettingsVm.WhenAnyValue(x => x.ShortDescription),
                _patcherSettingsVm.WhenAnyValue(x => x.LongDescription),
                _patcherSettingsVm.WhenAnyValue(x => x.Visibility),
                _patcherSettingsVm.WhenAnyValue(x => x.Versioning),
                _patcherSettingsVm.RequiredMods.ToObservableChangeSet()
                    .AddKey(x => x)
                    .Sort(ModKey.Alphabetical, SortOptimisations.ComparesImmutableValuesOnly, resetThreshold: 0)
                    .QueryWhenChanged()
                    .Select(x => x.Items)
                    .StartWith(Enumerable.Empty<ModKey>()),
                MetaPath,
                (nickname, shortDesc, desc, visibility, versioning, reqMods, meta) => (nickname, shortDesc, desc, visibility, versioning, reqMods: reqMods.Select(x => x.FileName).OrderBy(x => x).ToArray(), meta))
            .DistinctUntilChanged()
            .Throttle(TimeSpan.FromMilliseconds(200), RxApp.MainThreadScheduler)
            .Skip(1)
            .Subscribe(x =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(x.meta)) return;
                    _fileSystem.File.WriteAllText(x.meta,
                        JsonConvert.SerializeObject(
                            new PatcherCustomization()
                            {
                                OneLineDescription = x.shortDesc,
                                LongDescription = x.desc,
                                Visibility = x.visibility,
                                Nickname = x.nickname,
                                PreferredAutoVersioning = x.versioning,
                                RequiredMods = x.reqMods.Select(x => x.String).ToArray()
                            },
                            Formatting.Indented,
                            Execution.Constants.JsonSettings));
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Error writing out meta");
                }
            })
            .DisposeWith(disp);
        return disp;
    }
}