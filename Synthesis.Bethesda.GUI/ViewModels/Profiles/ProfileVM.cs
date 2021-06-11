using DynamicData;
using Mutagen.Bethesda;
using Newtonsoft.Json;
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
using Mutagen.Bethesda.WPF.Plugins.Order;
using Serilog;
using StructureMap;
using Synthesis.Bethesda.GUI.Profiles.Plugins;
using Synthesis.Bethesda.GUI.Services;
using Synthesis.Bethesda.GUI.Settings;

namespace Synthesis.Bethesda.GUI
{
    public class ProfileVM : ViewModel
    {
        private readonly PatcherInitializationVM _Init;
        private readonly INavigateTo _Navigate;
        private readonly IRetrieveSaveSettings _RetrieveSaveSettings;

        public GameRelease Release { get; }

        public SourceList<PatcherVM> Patchers { get; }

        public ICommand AddGitPatcherCommand { get; }
        public ICommand AddSolutionPatcherCommand { get; }
        public ICommand AddCliPatcherCommand { get; }
        public ICommand GoToErrorCommand { get; }
        public ICommand EnableAllPatchersCommand { get; }
        public ICommand DisableAllPatchersCommand { get; }
        public ICommand ExportCommand { get; }
        public ReactiveCommand<Unit, Unit> UpdateAllPatchersCommand { get; }

        public string ID { get; private set; }

        public string Nickname { get; }

        public string ProfileDirectory { get; }
        public string WorkingDirectory { get; }

        public IProfileDataFolder DataFolderOverride { get; }
        public IProfileVersioning Versioning { get; }

        private readonly ObservableAsPropertyHelper<string> _DataFolder;
        public string DataFolder => _DataFolder.Value;

        private readonly ObservableAsPropertyHelper<ErrorResponse> _BlockingError;
        public ErrorResponse BlockingError => _BlockingError.Value;

        private readonly ObservableAsPropertyHelper<GetResponse<PatcherVM>> _LargeOverallError;
        public GetResponse<PatcherVM> LargeOverallError => _LargeOverallError.Value;

        public IObservableList<ReadOnlyModListingVM> LoadOrder { get; }

        private readonly ObservableAsPropertyHelper<bool> _IsActive;
        public bool IsActive => _IsActive.Value;

        public ICommand SetAllToProfileCommand { get; }

        private readonly ObservableAsPropertyHelper<PatcherVM?> _SelectedPatcher;
        public PatcherVM? SelectedPatcher => _SelectedPatcher.Value;

        [Reactive]
        public bool ConsiderPrereleaseNugets { get; set; }

        public IProfileDisplayControllerVm DisplayController { get; }

        public ErrorVM OverallErrorVM { get; } = new ErrorVM("Overall Blocking Error");

        public IObservable<ILinkCache?> SimpleLinkCache { get; }

        public ILockToCurrentVersioning LockSetting { get; }
        public IPatcherFactory PatcherFactory { get; }

        [Reactive]
        public PersistenceMode SelectedPersistenceMode { get; set; } = PersistenceMode.Text;
        
        public ProfileVM(
            IProfilePatchersList patchersList,
            IProfileDataFolder dataFolder,
            PatcherInitializationVM init,
            IProfileIdentifier ident,
            IProfileLoadOrder loadOrder,
            IProfileDirectories dirs,
            IProfileVersioning versioning,
            INavigateTo navigate,
            IProfileDisplayControllerVm profileDisplay,
            ILockToCurrentVersioning lockSetting,
            GitPatcherInitVM gitPatcherInitVm,
            SolutionPatcherInitVM solutionPatcherInitVm,
            CliPatcherInitVM cliPatcherInitVm,
            IRetrieveSaveSettings retrieveSaveSettings,
            IPatcherFactory patcherFactory,
            ISelectedProfileControllerVm selProfile,
            ILogger logger)
        {
            _Init = init;
            DataFolderOverride = dataFolder;
            Versioning = versioning;
            Patchers = patchersList.Patchers;
            LockSetting = lockSetting;
            PatcherFactory = patcherFactory;
            DisplayController = profileDisplay;
            _Navigate = navigate;
            _RetrieveSaveSettings = retrieveSaveSettings;
            Nickname = ident.Nickname;
            ID = ident.ID;
            Release = ident.Release;
            AddGitPatcherCommand = ReactiveCommand.Create(() =>
            {
                SetInitializer(gitPatcherInitVm);
            });
            AddSolutionPatcherCommand = ReactiveCommand.Create(() => SetInitializer(solutionPatcherInitVm));
            AddCliPatcherCommand = ReactiveCommand.Create(() =>
            {
                try
                {
                    SetInitializer(cliPatcherInitVm);
                }
                catch (Exception e)
                {
                    logger.Error(e, "Failed to create new Cli Patcher.");
                }
            });

            ProfileDirectory = dirs.ProfileDirectory;
            WorkingDirectory = dirs.WorkingDirectory;

            Patchers.Connect()
                .OnItemRemoved(p =>
                {
                    Log.Logger.Information($"Disposing of {p.DisplayName} because it was removed.");
                    p.Dispose();
                })
                .Subscribe()
                .DisposeWith(this);

            _DataFolder = dataFolder.WhenAnyValue(x => x.DataFolder)
                .ToGuiProperty<string>(this, nameof(DataFolder), string.Empty);

            LoadOrder = loadOrder.LoadOrder;

            _LargeOverallError = Observable.CombineLatest(
                    dataFolder.DataFolderResult,
                    loadOrder.WhenAnyValue(x => x.State),
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
                        .Filter(x => x.ModKey != Constants.SynthesisModKey)
                        .ObserveOnGui()
                        .FilterOnObservable(
                            x => x.WhenAnyValue(y => y.Exists)
                                .DistinctUntilChanged()
                                .Select(x => !x), 
                            scheduler: RxApp.MainThreadScheduler)
                        .QueryWhenChanged(q => q)
                        .StartWith(Noggog.ListExt.Empty<ReadOnlyModListingVM>())
                        .Throttle(TimeSpan.FromMilliseconds(200), RxApp.MainThreadScheduler),
                    (dataFolder, loadOrder, enabledPatchers, erroredEnabledPatchers, missingMods) =>
                    {
                        if (enabledPatchers.Count == 0) return GetResponse<PatcherVM>.Fail("There are no enabled patchers to run.");
                        if (!dataFolder.Succeeded) return dataFolder.BubbleFailure<PatcherVM>();
                        if (!loadOrder.Succeeded) return loadOrder.BubbleFailure<PatcherVM>();
                        if (missingMods.Count > 0)
                        {
                            return GetResponse<PatcherVM>.Fail($"Load order had mods that were missing:{Environment.NewLine}{string.Join(Environment.NewLine, missingMods.Select(x => x.ModKey))}");
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

            _IsActive = selProfile.WhenAnyValue(x => x.SelectedProfile)
                .Select(x => x == this)
                .ToGuiProperty(this, nameof(IsActive));

            GoToErrorCommand = NoggogCommand.CreateFromObject(
                objectSource: this.WhenAnyValue(x => x.LargeOverallError),
                canExecute: o => o.Failed,
                execute: o =>
                {
                    if (o.Value.TryGet(out var patcher))
                    {
                        DisplayController.SelectedObject = patcher;
                    }
                    else
                    {
                        var curDisplayed = DisplayController.SelectedObject;
                        if (!(curDisplayed is ErrorVM))
                        {
                            OverallErrorVM.BackAction = () => DisplayController.SelectedObject = curDisplayed;
                        }
                        else
                        {
                            OverallErrorVM.BackAction = null;
                        }
                        DisplayController.SelectedObject = OverallErrorVM;
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
            
            _SelectedPatcher = this.WhenAnyValue(x => x.DisplayController.SelectedObject)
                .Select(x => x as PatcherVM)
                .ToGuiProperty(this, nameof(SelectedPatcher), default);

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
                .ChangeNotNull()
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
                        .Select(q => q.Where(x => x.Enabled).Select(x => x.ModKey).ToArray())
                        .StartWithEmpty(),
                    (dataFolder, rel, loadOrder) => (dataFolder, rel, loadOrder))
                .Throttle(TimeSpan.FromMilliseconds(100), RxApp.TaskpoolScheduler)
                .Select(x =>
                {
                    return Observable.Create<ILinkCache?>(obs =>
                    {
                        try
                        {
                            var loadOrder = Mutagen.Bethesda.Plugins.Order.LoadOrder.Import(
                                x.dataFolder,
                                x.loadOrder,
                                factory: (modPath) => ModInstantiator.Importer(modPath, x.rel));
                            obs.OnNext(loadOrder.ToUntypedImmutableLinkCache(LinkCachePreferences.OnlyIdentifiers()));
                        }
                        catch (Exception ex)
                        {
                            Log.Logger.Error("Error creating simple link cache for GUI lookups", ex);
                            obs.OnNext(null);
                        }
                        obs.OnCompleted();
                        return Disposable.Empty;
                    });
                })
                .Switch()
                .Replay(1)
                .RefCount();

            ExportCommand = ReactiveCommand.Create(Export);
        }
        
        public SynthesisProfile Save()
        {
            return new SynthesisProfile()
            {
                Patchers = Patchers.Items.Select(p => p.Save()).ToList(),
                ID = ID,
                Nickname = Nickname,
                TargetRelease = Release,
                MutagenManualVersion = Versioning.ManualMutagenVersion,
                SynthesisManualVersion = Versioning.ManualSynthesisVersion,
                MutagenVersioning = Versioning.MutagenVersioning,
                SynthesisVersioning = Versioning.SynthesisVersioning,
                DataPathOverride = DataFolderOverride.DataPathOverride,
                ConsiderPrereleaseNugets = ConsiderPrereleaseNugets,
                LockToCurrentVersioning = LockSetting.Lock,
                Persistence = SelectedPersistenceMode,
            };
        }

        private void SetInitializer(PatcherInitVM initializer)
        {
            _Init.NewPatcher = initializer;
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
                _RetrieveSaveSettings.Retrieve(out var guiSettings, out var pipeSettings);
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
                _Navigate.Navigate(subDir);
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "Error while exporting settings");
            }
        }
    }
}
