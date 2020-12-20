using DynamicData;
using Microsoft.WindowsAPICodePack.Dialogs;
using Synthesis.Bethesda.Execution.Settings;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Text;
using System.Linq;
using DynamicData.Binding;
using System.Windows.Input;
using System.Diagnostics;
using System.Reactive;
using Newtonsoft.Json;
using Synthesis.Bethesda.DTO;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Patchers;
using Mutagen.Bethesda;

namespace Synthesis.Bethesda.GUI
{
    public class SolutionPatcherVM : PatcherVM
    {
        public PathPickerVM SolutionPath { get; } = new PathPickerVM()
        {
            ExistCheckOption = PathPickerVM.CheckOptions.On,
            PathType = PathPickerVM.PathTypeOptions.File,
        };

        public IObservableCollection<string> AvailableProjects { get; }

        [Reactive]
        public string ProjectSubpath { get; set; } = string.Empty;

        public PathPickerVM SelectedProjectPath { get; } = new PathPickerVM()
        {
            ExistCheckOption = PathPickerVM.CheckOptions.On,
            PathType = PathPickerVM.PathTypeOptions.File,
        };

        private readonly ObservableAsPropertyHelper<string> _DisplayName;
        public override string DisplayName => _DisplayName.Value;

        private readonly ObservableAsPropertyHelper<ConfigurationState> _State;
        public override ConfigurationState State => _State.Value;

        public ICommand OpenSolutionCommand { get; }

        [Reactive]
        public string ShortDescription { get; set; } = string.Empty;

        [Reactive]
        public string LongDescription { get; set; } = string.Empty;

        [Reactive]
        public VisibilityOptions Visibility { get; set; }

        [Reactive]
        public PreferredAutoVersioning Versioning { get; set; }

        public ObservableCollectionExtended<PreferredAutoVersioning> VersioningOptions { get; } = new ObservableCollectionExtended<PreferredAutoVersioning>(EnumExt.GetValues<PreferredAutoVersioning>());

        public ObservableCollectionExtended<VisibilityOptions> VisibilityOptions { get; } = new ObservableCollectionExtended<VisibilityOptions>(EnumExt.GetValues<VisibilityOptions>());

        public SourceCache<ModKey, ModKey> RequiredMods { get; } = new SourceCache<ModKey, ModKey>(x => x);

        public IObservableCollection<RequiredModVM> RequiredModsDisplay { get; }

        public IObservableCollection<DetectedModVM> DetectedMods { get; }

        [Reactive]
        public string AddRequiredModInput { get; set; } = string.Empty;

        public ICommand AddRequiredModCommand { get; }

        public ICommand ClearSearchCommand { get; }

        [Reactive]
        public string DetectedModsSearch { get; set; } = string.Empty;

        public SolutionPatcherVM(ProfileVM parent, SolutionPatcherSettings? settings = null)
            : base(parent, settings)
        {
            CopyInSettings(settings);
            SolutionPath.Filters.Add(new CommonFileDialogFilter("Solution", ".sln"));
            SelectedProjectPath.Filters.Add(new CommonFileDialogFilter("Project", ".csproj"));

            _DisplayName = Observable.CombineLatest(
                this.WhenAnyValue(x => x.Nickname),
                this.WhenAnyValue(x => x.SelectedProjectPath.TargetPath)
                    .StartWith(settings?.ProjectSubpath ?? string.Empty),
                (nickname, path) =>
                {
                    if (!string.IsNullOrWhiteSpace(nickname)) return nickname;
                    try
                    {
                        var name = Path.GetFileName(Path.GetDirectoryName(path));
                        if (string.IsNullOrWhiteSpace(name)) return string.Empty;
                        return name;
                    }
                    catch (Exception)
                    {
                        return string.Empty;
                    }
                })
                .ToProperty(this, nameof(DisplayName), Nickname);

            AvailableProjects = SolutionPatcherConfigLogic.AvailableProject(
                this.WhenAnyValue(x => x.SolutionPath.TargetPath))
                .ObserveOnGui()
                .ToObservableCollection(this);

            RequiredModsDisplay = RequiredMods.Connect()
                .Sort(ModKey.Alphabetical, SortOptimisations.ComparesImmutableValuesOnly, resetThreshold: 0)
                .Transform(x => new RequiredModVM(x, this))
                .ToObservableCollection(this.CompositeDisposable);

            var projPath = SolutionPatcherConfigLogic.ProjectPath(
                solutionPath: this.WhenAnyValue(x => x.SolutionPath.TargetPath),
                projectSubpath: this.WhenAnyValue(x => x.ProjectSubpath));
            projPath
                .Subscribe(p => SelectedProjectPath.TargetPath = p)
                .DisposeWith(this);

            _State = Observable.CombineLatest(
                    this.WhenAnyValue(x => x.SolutionPath.ErrorState),
                    this.WhenAnyValue(x => x.SelectedProjectPath.ErrorState),
                    this.WhenAnyValue(x => x.Profile.Config.MainVM)
                        .Select(x => x.DotNetSdkInstalled)
                        .Switch(),
                    (sln, proj, dotnet) =>
                    {
                        if (sln.Failed) return new ConfigurationState(sln);
                        if (dotnet == null) return new ConfigurationState(ErrorResponse.Fail("No dotnet SDK installed"));
                        return new ConfigurationState(proj);
                    })
                .ToGuiProperty<ConfigurationState>(this, nameof(State), new ConfigurationState(ErrorResponse.Fail("Evaluating"))
                {
                    IsHaltingError = false
                });

            OpenSolutionCommand = ReactiveCommand.Create(
                canExecute: this.WhenAnyValue(x => x.SolutionPath.InError)
                    .Select(x => !x),
                execute: () =>
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo(SolutionPath.TargetPath)
                        {
                            UseShellExecute = true,
                        });
                    }
                    catch (Exception ex)
                    {
                        Log.Logger.Error(ex, $"Error opening solution: {SolutionPath.TargetPath}");
                    }
                });

            var metaPath = this.WhenAnyValue(x => x.SelectedProjectPath.TargetPath)
                .Select(projPath =>
                {
                    try
                    {
                        return Path.Combine(Path.GetDirectoryName(projPath)!, Constants.MetaFileName);
                    }
                    catch (Exception)
                    {
                        return string.Empty;
                    }
                })
                .Replay(1)
                .RefCount();

            // Set up meta file sync
            metaPath
                .Select(path =>
                {
                    return Noggog.ObservableExt.WatchFile(path)
                        .StartWith(Unit.Default)
                        .Throttle(TimeSpan.FromMilliseconds(500), RxApp.MainThreadScheduler)
                        .Select(_ =>
                        {
                            if (!File.Exists(path)) return default;
                            try
                            {
                                return JsonConvert.DeserializeObject<PatcherCustomization>(
                                    File.ReadAllText(path),
                                    Execution.Constants.JsonSettings);
                            }
                            catch (Exception ex)
                            {
                                Logger.Error(ex, "Error reading in meta");
                            }
                            return default(PatcherCustomization?);
                        });
                })
                .Switch()
                .DistinctUntilChanged()
                .ObserveOnGui()
                .Subscribe(info =>
                {
                    if (info == null) return;
                    if (info.Nickname != null)
                    {
                        this.Nickname = info.Nickname;
                    }
                    this.LongDescription = info.LongDescription ?? string.Empty;
                    this.ShortDescription = info.OneLineDescription ?? string.Empty;
                    this.Visibility = info.Visibility;
                    this.Versioning = info.PreferredAutoVersioning;
                    this.RequiredMods.SetTo(info.RequiredMods
                        .SelectWhere(x => TryGet<ModKey>.Create(ModKey.TryFromNameAndExtension(x, out var modKey), modKey)));
                })
                .DisposeWith(this);

            Observable.CombineLatest(
                    this.WhenAnyValue(x => x.DisplayName),
                    this.WhenAnyValue(x => x.ShortDescription),
                    this.WhenAnyValue(x => x.LongDescription),
                    this.WhenAnyValue(x => x.Visibility),
                    this.WhenAnyValue(x => x.Versioning),
                    this.RequiredMods
                        .Connect()
                        .Sort(ModKey.Alphabetical, SortOptimisations.ComparesImmutableValuesOnly, resetThreshold: 0)
                        .QueryWhenChanged(),
                    metaPath,
                    (nickname, shortDesc, desc, visibility, versioning, reqMods, meta) => (nickname, shortDesc, desc, visibility, versioning, reqMods: reqMods.Items.Select(x => x.FileName).OrderBy(x => x).ToArray(), meta))
                .DistinctUntilChanged()
                .Throttle(TimeSpan.FromMilliseconds(200), RxApp.MainThreadScheduler)
                .Skip(1)
                .Subscribe(x =>
                {
                    try
                    {
                        if (string.IsNullOrWhiteSpace(x.meta)) return;
                        File.WriteAllText(x.meta,
                            JsonConvert.SerializeObject(
                                new PatcherCustomization()
                                {
                                    OneLineDescription = x.shortDesc,
                                    LongDescription = x.desc,
                                    Visibility = x.visibility,
                                    Nickname = x.nickname,
                                    PreferredAutoVersioning = x.versioning,
                                    RequiredMods = x.reqMods
                                },
                                Formatting.Indented,
                                Execution.Constants.JsonSettings));
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "Error writing out meta");
                    }
                })
                .DisposeWith(this);

            DetectedMods = this.WhenAnyValue(x => x.IsSelected)
                .Select(isSelected =>
                {
                    if (isSelected)
                    {
                        return this.Profile.LoadOrder.Connect()
                            .Transform(x => x.ModKey)
                            .AddKey(x => x)
                            .Except(RequiredMods.Connect());
                    }
                    else
                    {
                        return Observable.Empty<IChangeSet<ModKey, ModKey>>();
                    }
                })
                .Switch()
                .Filter(this.WhenAnyValue(x => x.DetectedModsSearch)
                    .Debounce(TimeSpan.FromMilliseconds(350), RxApp.MainThreadScheduler)
                    .Select(x => x.Trim())
                    .DistinctUntilChanged()
                    .Select(search =>
                    {
                        if (string.IsNullOrWhiteSpace(search))
                        {
                            return new Func<ModKey, bool>(_ => true);
                        }
                        return new Func<ModKey, bool>(
                            (p) =>
                            {
                                if (p.FileName.Contains(search, StringComparison.OrdinalIgnoreCase)) return true;
                                return false;
                            });
                    }))
                .Transform(x => new DetectedModVM(x, this))
                .ToObservableCollection(this.CompositeDisposable);

            AddRequiredModCommand = NoggogCommand.CreateFromObject(
                objectSource: this.WhenAnyValue(x => x.AddRequiredModInput)
                    .Select(x => TryGet<ModKey>.Create(ModKey.TryFromNameAndExtension(x, out var modKey), modKey)),
                canExecute: x => x.Succeeded,
                execute: x =>
                {
                    RequiredMods.AddOrUpdate(x.Value);
                    AddRequiredModInput = string.Empty;
                },
                disposable: this.CompositeDisposable);

            ClearSearchCommand = ReactiveCommand.Create(() => DetectedModsSearch = string.Empty);
        }

        public override PatcherSettings Save()
        {
            var ret = new SolutionPatcherSettings();
            CopyOverSave(ret);
            ret.SolutionPath = this.SolutionPath.TargetPath;
            ret.ProjectSubpath = this.ProjectSubpath;
            return ret;
        }

        private void CopyInSettings(SolutionPatcherSettings? settings)
        {
            if (settings == null) return;
            this.SolutionPath.TargetPath = settings.SolutionPath;
            this.ProjectSubpath = settings.ProjectSubpath;
        }

        public override PatcherRunVM ToRunner(PatchersRunVM parent)
        {
            return new PatcherRunVM(
                parent,
                this,
                new SolutionPatcherRun(
                    name: DisplayName,
                    pathToSln: SolutionPath.TargetPath,
                    pathToExtraDataBaseFolder: Execution.Constants.TypicalExtraData,
                    pathToProj: SelectedProjectPath.TargetPath));
        }

        public class SolutionPatcherConfigLogic
        {
            public static IObservable<IChangeSet<string>> AvailableProject(IObservable<string> solutionPath)
            {
                return solutionPath
                    .ObserveOn(RxApp.TaskpoolScheduler)
                    .Select(SolutionPatcherRun.AvailableProjects)
                    .Select(x => x.AsObservableChangeSet())
                    .Switch()
                    .RefCount();
            }

            public static IObservable<string> ProjectPath(IObservable<string> solutionPath, IObservable<string> projectSubpath)
            {
                return projectSubpath
                    // Need to throttle, as bindings flip to null quickly, which we want to skip
                    .Throttle(TimeSpan.FromMilliseconds(150), RxApp.MainThreadScheduler)
                    .DistinctUntilChanged()
                    .CombineLatest(solutionPath.DistinctUntilChanged(),
                        (subPath, slnPath) =>
                        {
                            if (subPath == null || slnPath == null) return string.Empty;
                            try
                            {
                                return Path.Combine(Path.GetDirectoryName(slnPath)!, subPath);
                            }
                            catch (Exception)
                            {
                                return string.Empty;
                            }
                        })
                    .Replay(1)
                    .RefCount();
            }
        }
    }

    public class DetectedModVM : ViewModel
    {
        public ModKey ModKey { get; }
        public SolutionPatcherVM Patcher { get; }
        public ICommand AddAsRequiredModCommand { get; }

        public DetectedModVM(ModKey modKey, SolutionPatcherVM slnPatcher)
        {
            Patcher = slnPatcher;
            ModKey = modKey;
            AddAsRequiredModCommand = ReactiveCommand.Create(() => slnPatcher.RequiredMods.AddOrUpdate(modKey));
        }
    }

    public class RequiredModVM : ViewModel
    {
        public ModKey ModKey { get; }
        public SolutionPatcherVM Patcher { get; }
        public ICommand RemoveAsRequiredModCommand { get; }

        public RequiredModVM(ModKey modKey, SolutionPatcherVM slnPatcher)
        {
            Patcher = slnPatcher;
            ModKey = modKey;
            RemoveAsRequiredModCommand = ReactiveCommand.Create(() => slnPatcher.RequiredMods.RemoveKey(modKey));
        }
    }
}
