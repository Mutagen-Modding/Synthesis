using DynamicData;
using Mutagen.Bethesda;
using Newtonsoft.Json;
using Mutagen.Bethesda.Synthesis.WPF;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Settings;
using System;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins.Records;

namespace Synthesis.Bethesda.GUI
{
    public class ProfileVM : ViewModel
    {
        public ConfigurationVM Config { get; }
        public GameRelease Release { get; }

        public SourceList<PatcherVM> Patchers { get; } = new SourceList<PatcherVM>();

        public ICommand AddGitPatcherCommand { get; }
        public ICommand AddSolutionPatcherCommand { get; }
        public ICommand AddCliPatcherCommand { get; }
        public ICommand GoToErrorCommand { get; }
        public IReactiveCommand UpdateProfileNugetVersionCommand { get; }
        public ICommand EnableAllPatchersCommand { get; }
        public ICommand DisableAllPatchersCommand { get; }
        public ICommand ExportCommand { get; }
        public ReactiveCommand<Unit, Unit> UpdateAllPatchersCommand { get; }

        public string ID { get; private set; }

        [Reactive]
        public string Nickname { get; set; } = string.Empty;

        public string ProfileDirectory { get; }
        public string WorkingDirectory { get; }

        private readonly ObservableAsPropertyHelper<string> _DataFolder;
        public string DataFolder => _DataFolder.Value;

        private readonly ObservableAsPropertyHelper<ErrorResponse> _BlockingError;
        public ErrorResponse BlockingError => _BlockingError.Value;

        private readonly ObservableAsPropertyHelper<GetResponse<PatcherVM>> _LargeOverallError;
        public GetResponse<PatcherVM> LargeOverallError => _LargeOverallError.Value;

        public IObservableList<LoadOrderEntryVM> LoadOrder { get; }

        private readonly ObservableAsPropertyHelper<bool> _IsActive;
        public bool IsActive => _IsActive.Value;

        [Reactive]
        public NugetVersioningEnum MutagenVersioning { get; set; } = NugetVersioningEnum.Manual;

        [Reactive]
        public string? ManualMutagenVersion { get; set; }

        [Reactive]
        public NugetVersioningEnum SynthesisVersioning { get; set; } = NugetVersioningEnum.Manual;

        [Reactive]
        public string? ManualSynthesisVersion { get; set; }

        public IObservable<SynthesisNugetVersioning> ActiveVersioning { get; }

        public ICommand SetAllToProfileCommand { get; }

        public ReactiveCommand<Unit, Unit> UpdateMutagenManualToLatestCommand { get; }

        public ReactiveCommand<Unit, Unit> UpdateSynthesisManualToLatestCommand { get; }

        private readonly ObservableAsPropertyHelper<PatcherVM?> _SelectedPatcher;
        public PatcherVM? SelectedPatcher => _SelectedPatcher.Value;

        [Reactive]
        public bool ConsiderPrereleaseNugets { get; set; }

        [Reactive]
        public string? DataPathOverride { get; set; }

        [Reactive]
        public ViewModel? DisplayedObject { get; set; }

        public ErrorVM OverallErrorVM { get; } = new ErrorVM("Overall Blocking Error");

        public IObservable<ILinkCache> SimpleLinkCache { get; }

        [Reactive]
        public bool LockUpgrades { get; set; }

        [Reactive]
        public PersistenceMode SelectedPersistenceMode { get; set; } = PersistenceMode.Text;

        public ProfileVM(ConfigurationVM parent, GameRelease release, string id)
        {
            ID = id;
            Config = parent;
            Release = release;
            AddGitPatcherCommand = ReactiveCommand.Create(() => SetInitializer(new GitPatcherInitVM(this)));
            AddSolutionPatcherCommand = ReactiveCommand.Create(() => SetInitializer(new SolutionPatcherInitVM(this)));
            AddCliPatcherCommand = ReactiveCommand.Create(() => SetInitializer(new CliPatcherInitVM(this)));

            ProfileDirectory = Path.Combine(Execution.Paths.WorkingDirectory, ID);
            WorkingDirectory = Execution.Paths.ProfileWorkingDirectory(ID);

            Patchers.Connect()
                .OnItemRemoved(p =>
                {
                    Log.Logger.Information($"Disposing of {p.DisplayName} because it was removed.");
                    p.Dispose();
                })
                .Subscribe()
                .DisposeWith(this);

            var dataFolderResult = this.WhenAnyValue(x => x.DataPathOverride)
                .Select(path =>
                {
                    if (path != null) return Observable.Return(GetResponse<string>.Succeed(path));
                    Log.Logger.Information("Starting to locate data folder");
                    return this.WhenAnyValue(x => x.Release)
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
                // Watch folder for existance
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
                .Replay(1)
                .RefCount();

            _DataFolder = dataFolderResult
                .Select(x => x.Value)
                .ToGuiProperty<string>(this, nameof(DataFolder), string.Empty);

            dataFolderResult
                .Subscribe(d =>
                {
                    if (d.Failed)
                    {
                        Log.Logger.Error($"Could not locate data folder: {d.Reason}");
                    }
                    else
                    {
                        Log.Logger.Information($"Data Folder: {d.Value}");
                    }
                })
                .DisposeWith(this);

            var loadOrderResult = Observable.CombineLatest(
                    this.WhenAnyValue(x => x.Release),
                    dataFolderResult,
                    (release, dataFolder) => (release, dataFolder))
                .ObserveOn(RxApp.TaskpoolScheduler)
                .Select(x =>
                {
                    if (x.dataFolder.Failed)
                    {
                        return (Results: Observable.Empty<IChangeSet<LoadOrderEntryVM>>(), State: Observable.Return(ErrorResponse.Fail("Data folder not set")));
                    }
                    Log.Logger.Error($"Getting live load order for {x.release} -> {x.dataFolder.Value}");
                    var liveLo = Mutagen.Bethesda.Plugins.Order.LoadOrder.GetLiveLoadOrder(x.release, x.dataFolder.Value, out var errors)
                        .Transform(listing => new LoadOrderEntryVM(listing, x.dataFolder.Value))
                        .DisposeMany();
                    return (Results: liveLo, State: errors);
                })
                .StartWith((Results: Observable.Empty<IChangeSet<LoadOrderEntryVM>>(), State: Observable.Return(ErrorResponse.Fail("Load order uninitialized"))))
                .Replay(1)
                .RefCount();

            LoadOrder = loadOrderResult
                .Select(x => x.Results)
                .Switch()
                .AsObservableList();

            loadOrderResult.Select(lo => lo.State)
                .Switch()
                .Subscribe(loErr =>
                {
                    if (loErr.Succeeded)
                    {
                        Log.Logger.Information($"Load order location successful");
                    }
                    else
                    {
                        Log.Logger.Information($"Load order location error: {loErr.Reason}");
                    }
                })
                .DisposeWith(this);

            _LargeOverallError = Observable.CombineLatest(
                    dataFolderResult,
                    loadOrderResult
                        .Select(x => x.State)
                        .Switch(),
                    Patchers.Connect()
                        .ObserveOnGui()
                        .FilterOnObservable(p => p.WhenAnyValue(x => x.IsOn), scheduler: RxApp.MainThreadScheduler)
                        .QueryWhenChanged(q => q)
                        .StartWith(Noggog.ListExt.Empty<PatcherVM>()),
                    Patchers.Connect()
                        .ObserveOnGui()
                        .FilterOnObservable(p => Observable.CombineLatest(
                            p.WhenAnyValue(x => x.IsOn),
                            p.WhenAnyValue(x => x.State.IsHaltingError),
                            (on, halting) => on && halting), 
                            scheduler: RxApp.MainThreadScheduler)
                        .QueryWhenChanged(q => q)
                        .StartWith(Noggog.ListExt.Empty<PatcherVM>()),
                    LoadOrder.Connect()
                        .Filter(x => x.Listing.ModKey != Constants.SynthesisModKey)
                        .ObserveOnGui()
                        .FilterOnObservable(
                            x => x.WhenAnyValue(y => y.Exists)
                                .DistinctUntilChanged()
                                .Select(x => !x), 
                            scheduler: RxApp.MainThreadScheduler)
                        .QueryWhenChanged(q => q)
                        .StartWith(Noggog.ListExt.Empty<LoadOrderEntryVM>())
                        .Throttle(TimeSpan.FromMilliseconds(200), RxApp.MainThreadScheduler),
                    (dataFolder, loadOrder, enabledPatchers, erroredEnabledPatchers, missingMods) =>
                    {
                        if (enabledPatchers.Count == 0) return GetResponse<PatcherVM>.Fail("There are no enabled patchers to run.");
                        if (!dataFolder.Succeeded) return dataFolder.BubbleFailure<PatcherVM>();
                        if (!loadOrder.Succeeded) return loadOrder.BubbleFailure<PatcherVM>();
                        if (missingMods.Count > 0)
                        {
                            return GetResponse<PatcherVM>.Fail($"Load order had mods that were missing:{Environment.NewLine}{string.Join(Environment.NewLine, missingMods.Select(x => x.Listing.ModKey))}");
                        }
                        if (erroredEnabledPatchers.Count > 0)
                        {
                            var errPatcher = erroredEnabledPatchers.First();
                            return GetResponse<PatcherVM>.Fail(errPatcher, $"\"{errPatcher.DisplayName}\" has a blocking error: {errPatcher.State.RunnableState.Reason}");
                        }
                        return GetResponse<PatcherVM>.Succeed(null!);
                    })
                .Throttle(TimeSpan.FromMilliseconds(200), RxApp.MainThreadScheduler)
                .Do(x =>
                {
                    if (x.Failed)
                    {
                        Log.Logger.Warning($"Encountered blocking overall error: {x.Reason}");
                    }
                })
                .ToGuiProperty(this, nameof(LargeOverallError), GetResponse<PatcherVM>.Fail("Uninitialized overall error"));

            _BlockingError = Observable.CombineLatest(
                    this.WhenAnyValue(x => x.LargeOverallError),
                    Patchers.Connect()
                        .ObserveOnGui()
                        .AutoRefresh(x => x.IsOn, scheduler: RxApp.MainThreadScheduler)
                        .Filter(p => p.IsOn)
                        .AutoRefresh(x => x.State, scheduler: RxApp.MainThreadScheduler)
                        .Transform(p => p.State, transformOnRefresh: true)
                        .QueryWhenChanged(errs =>
                        {
                            var blocking = errs.Cast<ConfigurationState?>().FirstOrDefault<ConfigurationState?>(e => (!e?.RunnableState.Succeeded) ?? false);
                            if (blocking == null) return ErrorResponse.Success;
                            return blocking.RunnableState;
                        }),
                (overall, patchers) =>
                {
                    if (!overall.Succeeded) return overall;
                    return patchers;
                })
                .ToGuiProperty<ErrorResponse>(this, nameof(BlockingError), ErrorResponse.Fail("Uninitialized blocking error"));

            _IsActive = this.WhenAnyValue(x => x.Config.SelectedProfile)
                .Select(x => x == this)
                .ToGuiProperty(this, nameof(IsActive));

            GoToErrorCommand = NoggogCommand.CreateFromObject(
                objectSource: this.WhenAnyValue(x => x.LargeOverallError),
                canExecute: o => o.Failed,
                execute: o =>
                {
                    if (o.Value.TryGet(out var patcher))
                    {
                        DisplayedObject = patcher;
                    }
                    else
                    {
                        var curDisplayed = DisplayedObject;
                        if (!(curDisplayed is ErrorVM))
                        {
                            OverallErrorVM.BackAction = () => DisplayedObject = curDisplayed;
                        }
                        else
                        {
                            OverallErrorVM.BackAction = null;
                        }
                        DisplayedObject = OverallErrorVM;
                    }
                },
                disposable: this.CompositeDisposable);

            // Forward overall errors into VM
            this.WhenAnyValue(x => x.LargeOverallError)
                .Subscribe(err =>
                {
                    if (err.Succeeded || err.Value != null)
                    {
                        OverallErrorVM.String = null;
                    }
                    else
                    {
                        OverallErrorVM.String = err.Reason;
                    }
                })
                .DisposeWith(this);

            _SelectedPatcher = this.WhenAnyValue(x => x.DisplayedObject)
                .Select(x => x as PatcherVM)
                .ToGuiProperty(this, nameof(SelectedPatcher), default);

            ActiveVersioning = Observable.CombineLatest(
                    this.WhenAnyValue(x => x.MutagenVersioning),
                    this.WhenAnyValue(x => x.ManualMutagenVersion),
                    parent.MainVM.NewestMutagenVersion,
                    this.WhenAnyValue(x => x.SynthesisVersioning),
                    this.WhenAnyValue(x => x.ManualSynthesisVersion),
                    parent.MainVM.NewestSynthesisVersion,
                    (mutaVersioning, mutaManual, newestMuta, synthVersioning, synthManual, newestSynth) =>
                    {
                        return new SynthesisNugetVersioning(
                            new NugetVersioning("Mutagen", mutaVersioning, mutaManual ?? newestMuta ?? string.Empty, newestMuta),
                            new NugetVersioning("Synthesis", synthVersioning, synthManual ?? newestSynth ?? string.Empty, newestSynth));
                    })
                .Do(x => Log.Logger.Information($"Swapped profile {Nickname} to {x}"))
                .ObserveOnGui()
                .Replay(1)
                .RefCount();

            // Set manual if empty
            parent.MainVM.NewestMutagenVersion
                .Subscribe(x =>
                {
                    if (ManualMutagenVersion == null)
                    {
                        ManualMutagenVersion = x;
                    }
                })
                .DisposeWith(this);
            parent.MainVM.NewestSynthesisVersion
                .Subscribe(x =>
                {
                    if (ManualSynthesisVersion == null)
                    {
                        ManualSynthesisVersion = x;
                    }
                })
                .DisposeWith(this);

            SetAllToProfileCommand = ReactiveCommand.Create(
                execute: () =>
                {
                    foreach (var patcher in Patchers.Items)
                    {
                        if (patcher is GitPatcherVM gitPatcher)
                        {
                            gitPatcher.MutagenVersioning = PatcherNugetVersioningEnum.Profile;
                            gitPatcher.SynthesisVersioning = PatcherNugetVersioningEnum.Profile;
                        }
                    }
                });

            UpdateMutagenManualToLatestCommand = NoggogCommand.CreateFromObject(
                objectSource: parent.MainVM.NewestMutagenVersion
                    .ObserveOnGui(),
                canExecute: v =>
                {
                    return Observable.CombineLatest(
                            this.WhenAnyValue(x => x.MutagenVersioning),
                            this.WhenAnyValue(x => x.ManualMutagenVersion),
                            v,
                            (versioning, manual, latest) =>
                            {
                                if (versioning != NugetVersioningEnum.Manual) return false;
                                return latest != null && latest != manual;
                            })
                        .ObserveOnGui();
                },
                execute: v => ManualMutagenVersion = v ?? string.Empty,
                disposable: this.CompositeDisposable);
            UpdateSynthesisManualToLatestCommand = NoggogCommand.CreateFromObject(
                objectSource: parent.MainVM.NewestSynthesisVersion
                    .ObserveOnGui(),
                canExecute: v =>
                {
                    return Observable.CombineLatest(
                            this.WhenAnyValue(x => x.SynthesisVersioning),
                            this.WhenAnyValue(x => x.ManualSynthesisVersion),
                            v,
                            (versioning, manual, latest) =>
                            {
                                if (versioning != NugetVersioningEnum.Manual) return false;
                                return latest != null && latest != manual;
                            })
                        .ObserveOnGui();
                },
                execute: v => ManualSynthesisVersion = v ?? string.Empty,
                disposable: this.CompositeDisposable);

            UpdateProfileNugetVersionCommand = CommandExt.CreateCombinedAny(
                this.UpdateMutagenManualToLatestCommand,
                this.UpdateSynthesisManualToLatestCommand);

            EnableAllPatchersCommand = ReactiveCommand.Create(() =>
            {
                foreach (var patcher in this.Patchers.Items)
                {
                    patcher.IsOn = true;
                }
            });
            DisableAllPatchersCommand = ReactiveCommand.Create(() =>
            {
                foreach (var patcher in this.Patchers.Items)
                {
                    patcher.IsOn = false;
                }
            });
            var allCommands = Patchers.Connect()
                .Transform(x => x as GitPatcherVM)
                .NotNull()
                .Transform(x => CommandVM.Factory(x.UpdateAllCommand))
                .AsObservableList();
            UpdateAllPatchersCommand = ReactiveCommand.CreateFromTask(
                canExecute: allCommands.Connect()
                    .AutoRefresh(x => x.CanExecute)
                    .Filter(p => p.CanExecute)
                    .QueryWhenChanged(q => q.Count > 0),
                execute: () =>
                {
                    return Task.WhenAll(allCommands.Items
                        .Select(async cmd =>
                        {
                            try
                            {
                                if (cmd.CanExecute)
                                {
                                    await cmd.Command.Execute();
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Logger.Error(ex, "Error updating a patcher");
                            }
                        }));
                });

            SimpleLinkCache = Observable.CombineLatest(
                    this.WhenAnyValue(x => x.DataFolder),
                    this.WhenAnyValue(x => x.Release),
                    this.LoadOrder.Connect()
                        .QueryWhenChanged()
                        .Select(q => q.Where(x => x.Listing.Enabled).Select(x => x.Listing.ModKey).ToArray())
                        .StartWithEmpty(),
                    (dataFolder, rel, loadOrder) => (dataFolder, rel, loadOrder))
                .Throttle(TimeSpan.FromMilliseconds(100), RxApp.TaskpoolScheduler)
                .Select(x =>
                {
                    return Observable.Create<ILinkCache>(obs =>
                    {
                        var loadOrder = Mutagen.Bethesda.Plugins.Order.LoadOrder.Import(
                            x.dataFolder,
                            x.loadOrder,
                            factory: (modPath) => ModInstantiator.Importer(modPath, x.rel));
                        obs.OnNext(loadOrder.ToUntypedImmutableLinkCache(LinkCachePreferences.OnlyIdentifiers()));
                        obs.OnCompleted();
                        return Disposable.Empty;
                    });
                })
                .Switch()
                .Replay(1)
                .RefCount();

            ExportCommand = ReactiveCommand.Create(Export);
        }
           
        public ProfileVM(ConfigurationVM parent, SynthesisProfile settings)
            : this(parent, settings.TargetRelease, id: settings.ID)
        {
            Nickname = settings.Nickname;
            MutagenVersioning = settings.MutagenVersioning;
            ManualMutagenVersion = settings.MutagenManualVersion;
            SynthesisVersioning = settings.SynthesisVersioning;
            ManualSynthesisVersion = settings.SynthesisManualVersion;
            DataPathOverride = settings.DataPathOverride;
            ConsiderPrereleaseNugets = settings.ConsiderPrereleaseNugets;
            LockUpgrades = settings.LockToCurrentVersioning;
            SelectedPersistenceMode = settings.Persistence;
            Patchers.AddRange(settings.Patchers.Select<PatcherSettings, PatcherVM>(p =>
            {
                return p switch
                {
                    GithubPatcherSettings git => new GitPatcherVM(this, git),
                    SolutionPatcherSettings soln => new SolutionPatcherVM(this, soln),
                    CliPatcherSettings cli => new CliPatcherVM(this, cli),
                    _ => throw new NotImplementedException(),
                };
            }));
        }

        public SynthesisProfile Save()
        {
            return new SynthesisProfile()
            {
                Patchers = Patchers.Items.Select(p => p.Save()).ToList(),
                ID = ID,
                Nickname = Nickname,
                TargetRelease = Release,
                MutagenManualVersion = ManualMutagenVersion,
                SynthesisManualVersion = ManualSynthesisVersion,
                MutagenVersioning = MutagenVersioning,
                SynthesisVersioning = SynthesisVersioning,
                DataPathOverride = DataPathOverride,
                ConsiderPrereleaseNugets = ConsiderPrereleaseNugets,
                LockToCurrentVersioning = LockUpgrades,
                Persistence = SelectedPersistenceMode,
            };
        }

        private void SetPatcherForInitialConfiguration(PatcherVM patcher)
        {
            patcher.Profile.Patchers.Add(patcher);
            DisplayedObject = patcher;
        }

        private void SetInitializer(PatcherInitVM initializer)
        {
            Config.NewPatcher = initializer;
        }

        public override void Dispose()
        {
            base.Dispose();
            foreach (var patcher in Patchers.Items)
            {
                patcher.Dispose();
            }
        }

        public void Export()
        {
            try
            {
                Config.MainVM.Save(out var guiSettings, out var pipeSettings);
                pipeSettings.Profiles.RemoveWhere(p => p.ID != this.ID);
                guiSettings.SelectedProfile = this.ID;
                if (pipeSettings.Profiles.Count != 1)
                {
                    throw new ArgumentException("Unexpected number of profiles for export");
                }
                var profile = pipeSettings.Profiles[0];
                profile.LockToCurrentVersioning = true;
                foreach (var gitPatcher in profile.Patchers.WhereCastable<PatcherSettings, GithubPatcherSettings>())
                {
                    gitPatcher.AutoUpdateToBranchTip = false;
                    gitPatcher.LatestTag = false;
                }
                var subDir = "Export";
                Directory.CreateDirectory(subDir);
                File.WriteAllText(
                    Path.Combine(subDir, Execution.Paths.SettingsFileName),
                    JsonConvert.SerializeObject(pipeSettings, Formatting.Indented, Execution.Constants.JsonSettings));
                File.WriteAllText(
                    Path.Combine(subDir, Paths.GuiSettingsPath),
                    JsonConvert.SerializeObject(guiSettings, Formatting.Indented, Execution.Constants.JsonSettings));
                var dataDir = new DirectoryInfo("Data");
                if (dataDir.Exists)
                {
                    dataDir.DeepCopy(new DirectoryInfo(Path.Combine(subDir, "Data")));
                }
                Utility.NavigateToPath(subDir);
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "Error while exporting settings");
            }
        }
    }
}
