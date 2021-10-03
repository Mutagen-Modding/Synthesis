using System;
using System.IO.Abstractions;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using Mutagen.Bethesda.Plugins;
using Newtonsoft.Json;
using Noggog;
using Noggog.Reactive;
using ReactiveUI;
using Serilog;
using Synthesis.Bethesda.DTO;

namespace Synthesis.Bethesda.GUI.Services.Patchers.Solution
{
    public interface ISolutionMetaFileSync
    {
        IDisposable Sync();
    }

    public class SolutionMetaFileSync : ISolutionMetaFileSync
    {
        private readonly IFileSystem _FileSystem;
        private readonly ILogger _Logger;
        // private readonly IProvidePatcherMetaPath _MetaPath;
        // private readonly ISchedulerProvider _SchedulerProvider;
        // private readonly ISolutionPatcherSettingsVm _PatcherSettingsVm;

        public SolutionMetaFileSync(
            IFileSystem fileSystem,
            ILogger logger
            // ,
            // IProvidePatcherMetaPath metaPath,
            // ISchedulerProvider schedulerProvider,
            // ISolutionPatcherSettingsVm patcherSettingsVm
            )
        {
            _FileSystem = fileSystem;
            _Logger = logger;
            // _MetaPath = metaPath;
            // _SchedulerProvider = schedulerProvider;
            // _PatcherSettingsVm = patcherSettingsVm;
        }

        public IDisposable Sync()
        {
            var disp = new CompositeDisposable();
            // _MetaPath.Path
            //     .Select(path =>
            //     {
            //         return Noggog.ObservableExt.WatchFile(path)
            //             .Throttle(TimeSpan.FromMilliseconds(500), RxApp.MainThreadScheduler)
            //             .StartWith(Unit.Default)
            //             .Select(_ =>
            //             {
            //                 if (!_FileSystem.File.Exists(path)) return default;
            //                 try
            //                 {
            //                     return JsonConvert.DeserializeObject<PatcherCustomization>(
            //                         _FileSystem.File.ReadAllText(path),
            //                         Execution.Constants.JsonSettings);
            //                 }
            //                 catch (Exception ex)
            //                 {
            //                     _Logger.Error(ex, "Error reading in meta");
            //                 }
            //                 return default(PatcherCustomization?);
            //             });
            //     })
            //     .Switch()
            //     .DistinctUntilChanged()
            //     .ObserveOn(_SchedulerProvider.MainThread)
            //     .Subscribe(info =>
            //     {
            //         if (info == null) return;
            //         if (info.Nickname != null)
            //         {
            //             _PatcherSettingsVm.Nickname = info.Nickname;
            //         }
            //         _PatcherSettingsVm.LongDescription = info.LongDescription ?? string.Empty;
            //         _PatcherSettingsVm.ShortDescription = info.OneLineDescription ?? string.Empty;
            //         _PatcherSettingsVm.Visibility = info.Visibility;
            //         _PatcherSettingsVm.Versioning = info.PreferredAutoVersioning;
            //         _PatcherSettingsVm.SetRequiredMods(info.RequiredMods
            //             .SelectWhere(x => TryGet<ModKey>.Create(ModKey.TryFromNameAndExtension(x, out var modKey), modKey)));
            //     })
            //     .DisposeWith(disp);
            //
            // Observable.CombineLatest(
            //         _PatcherSettingsVm.WhenAnyValue(x => x.Nickname),
            //         _PatcherSettingsVm.WhenAnyValue(x => x.ShortDescription),
            //         _PatcherSettingsVm.WhenAnyValue(x => x.LongDescription),
            //         _PatcherSettingsVm.WhenAnyValue(x => x.Visibility),
            //         _PatcherSettingsVm.WhenAnyValue(x => x.Versioning),
            //         _PatcherSettingsVm.RequiredMods
            //             .AddKey(x => x)
            //             .Sort(ModKey.Alphabetical, SortOptimisations.ComparesImmutableValuesOnly, resetThreshold: 0)
            //             .QueryWhenChanged()
            //             .Select(x => x.Items)
            //             .StartWith(Enumerable.Empty<ModKey>()),
            //         _MetaPath.Path,
            //         (nickname, shortDesc, desc, visibility, versioning, reqMods, meta) => (nickname, shortDesc, desc, visibility, versioning, reqMods: reqMods.Select(x => x.FileName).OrderBy(x => x).ToArray(), meta))
            //     .DistinctUntilChanged()
            //     .Throttle(TimeSpan.FromMilliseconds(200), RxApp.MainThreadScheduler)
            //     .Skip(1)
            //     .Subscribe(x =>
            //     {
            //         try
            //         {
            //             if (string.IsNullOrWhiteSpace(x.meta)) return;
            //             _FileSystem.File.WriteAllText(x.meta,
            //                 JsonConvert.SerializeObject(
            //                     new PatcherCustomization()
            //                     {
            //                         OneLineDescription = x.shortDesc,
            //                         LongDescription = x.desc,
            //                         Visibility = x.visibility,
            //                         Nickname = x.nickname,
            //                         PreferredAutoVersioning = x.versioning,
            //                         RequiredMods = x.reqMods.Select(x => x.String).ToArray()
            //                     },
            //                     Formatting.Indented,
            //                     Execution.Constants.JsonSettings));
            //         }
            //         catch (Exception ex)
            //         {
            //             _Logger.Error(ex, "Error writing out meta");
            //         }
            //     })
            //     .DisposeWith(disp);
            return disp;
        }
    }
}