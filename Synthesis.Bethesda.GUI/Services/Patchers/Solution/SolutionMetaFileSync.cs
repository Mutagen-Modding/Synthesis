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
    private readonly ISolutionPatcherSettingsSyncTarget _patcherSettingsVm;
        
    public IObservable<string> MetaPath { get; }

    public SolutionMetaFileSync(
        IFileSystem fileSystem,
        ILogger logger,
        ISchedulerProvider schedulerProvider,
        IPatcherNameVm nameVm,
        ISelectedProjectInputVm selectedProjectInput,
        ISolutionPatcherSettingsSyncTarget patcherSettingsVm)
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
            .Select(x => x)
            .DistinctUntilChanged()
            .ObserveOn(_schedulerProvider.MainThread)
            .Subscribe(info =>
            {
                if (info == null) return;
                if (info.Nickname != null)
                {
                    _nameVm.Nickname = info.Nickname;
                }
                _patcherSettingsVm.Update(info);
            })
            .DisposeWith(disp);
        
        Observable.CombineLatest(
                _nameVm.WhenAnyValue(x => x.Nickname),
                _patcherSettingsVm.Updated,
                MetaPath,
                (nickname, slnSettings, meta) => (nickname, slnSettings, meta))
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
                                OneLineDescription = x.slnSettings.OneLineDescription,
                                LongDescription = x.slnSettings.LongDescription,
                                Visibility = x.slnSettings.Visibility,
                                Nickname = x.nickname,
                                PreferredAutoVersioning = x.slnSettings.PreferredAutoVersioning,
                                RequiredMods = x.slnSettings.RequiredMods.ToArray()
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