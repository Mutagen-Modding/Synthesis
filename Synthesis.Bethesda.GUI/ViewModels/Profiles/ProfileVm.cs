using System;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Autofac;
using DynamicData;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.WPF.Plugins.Order;
using Newtonsoft.Json;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Pathing;
using Synthesis.Bethesda.Execution.Profile;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.GUI.Services.Main;
using Synthesis.Bethesda.GUI.Settings;
using Synthesis.Bethesda.GUI.ViewModels.Patchers;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Git;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.Initialization;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.Plugins;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.Running;
using Synthesis.Bethesda.GUI.ViewModels.Top;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles
{
    public class ProfileVm : ViewModel
    {
        private readonly IRunFactory _RunFactory;
        private readonly INavigateTo _Navigate;
        private readonly IRetrieveSaveSettings _RetrieveSaveSettings;
        private readonly IPipelineSettingsPath _PipelinePaths;
        private readonly IGuiSettingsPath _GuiPaths;
        private readonly ILogger _Logger;

        public GameRelease Release { get; }

        public SourceList<PatcherVm> Patchers { get; }

        public ICommand GoToErrorCommand { get; }
        public ICommand EnableAllPatchersCommand { get; }
        public ICommand DisableAllPatchersCommand { get; }
        public ICommand ExportCommand { get; }
        public ReactiveCommand<Unit, Unit> UpdateAllPatchersCommand { get; }

        public string ID { get; private set; }

        public string Nickname { get; }

        public string ProfileDirectory { get; }
        public string WorkingDirectory { get; }

        public IProfileDataFolderVm DataFolderOverride { get; }
        public IProfileVersioning Versioning { get; }

        private readonly ObservableAsPropertyHelper<DirectoryPath> _DataFolder;
        public DirectoryPath DataFolder => _DataFolder.Value;

        private readonly ObservableAsPropertyHelper<ErrorResponse> _BlockingError;
        public ErrorResponse BlockingError => _BlockingError.Value;

        private readonly ObservableAsPropertyHelper<GetResponse<PatcherVm>> _LargeOverallError;
        public GetResponse<PatcherVm> LargeOverallError => _LargeOverallError.Value;

        public IObservableList<ReadOnlyModListingVM> LoadOrder { get; }

        private readonly ObservableAsPropertyHelper<bool> _IsActive;
        public bool IsActive => _IsActive.Value;

        public ICommand SetAllToProfileCommand { get; }

        private readonly ObservableAsPropertyHelper<PatcherVm?> _SelectedPatcher;
        public PatcherVm? SelectedPatcher => _SelectedPatcher.Value;

        [Reactive]
        public bool ConsiderPrereleaseNugets { get; set; }

        public IProfileDisplayControllerVm DisplayController { get; }

        public ErrorVM OverallErrorVm { get; } = new ErrorVM("Overall Blocking Error");

        public IObservable<ILinkCache?> SimpleLinkCache { get; }

        public ILockToCurrentVersioning LockSetting { get; }

        [Reactive]
        public PersistenceMode SelectedPersistenceMode { get; set; } = PersistenceMode.Text;

        public ILifetimeScope Scope { get; }
        public IPatcherInitializationFactoryVm Init { get; }
        
        public ProfileVm(
            ILifetimeScope scope,
            IRunFactory runFactory,
            IProfilePatchersList patchersList,
            IProfileDataFolderVm dataFolder,
            IPatcherInitializationFactoryVm init,
            IProfileIdentifier ident,
            IProfileNameProvider nameProvider,
            IProfileLoadOrder loadOrder,
            IProfileDirectories dirs,
            IProfileVersioning versioning,
            INavigateTo navigate,
            IProfileDisplayControllerVm profileDisplay,
            ILockToCurrentVersioning lockSetting,
            IRetrieveSaveSettings retrieveSaveSettings,
            ISelectedProfileControllerVm selProfile,
            IPipelineSettingsPath pipelineSettingsPath,
            IGuiSettingsPath guiPaths,
            ILogger logger)
        {
            Scope = scope;
            Init = init;
            DataFolderOverride = dataFolder;
            Versioning = versioning;
            Patchers = patchersList.Patchers;
            LockSetting = lockSetting;
            DisplayController = profileDisplay;
            _RunFactory = runFactory;
            _Navigate = navigate;
            _RetrieveSaveSettings = retrieveSaveSettings;
            _PipelinePaths = pipelineSettingsPath;
            _GuiPaths = guiPaths;
            _Logger = logger;
            Nickname = nameProvider.Name;
            ID = ident.ID;
            Release = ident.Release;

            ProfileDirectory = dirs.ProfileDirectory;
            WorkingDirectory = dirs.WorkingDirectory;

            Patchers.Connect()
                .OnItemRemoved(p =>
                {
                    logger.Information($"Disposing of {p.NameVm.Name} because it was removed.");
                    p.Dispose();
                })
                .Subscribe()
                .DisposeWith(this);

            _DataFolder = dataFolder.WhenAnyValue(x => x.Path)
                .ToGuiProperty<DirectoryPath>(this, nameof(DataFolder), string.Empty);

            LoadOrder = loadOrder.LoadOrder;

            _LargeOverallError = Observable.CombineLatest(
                    dataFolder.WhenAnyValue(x => x.DataFolderResult),
                    loadOrder.WhenAnyValue(x => x.State),
                    Patchers.Connect()
                        .ObserveOnGui()
                        .FilterOnObservable(p => p.WhenAnyValue(x => x.IsOn), scheduler: RxApp.MainThreadScheduler)
                        .QueryWhenChanged(q => q)
                        .StartWith(Noggog.ListExt.Empty<PatcherVm>()),
                    Patchers.Connect()
                        .ObserveOnGui()
                        .FilterOnObservable(p => Observable.CombineLatest(
                            p.WhenAnyValue(x => x.IsOn),
                            p.WhenAnyValue(x => x.State.IsHaltingError),
                            (on, halting) => on && halting), 
                            scheduler: RxApp.MainThreadScheduler)
                        .QueryWhenChanged(q => q)
                        .StartWith(Noggog.ListExt.Empty<PatcherVm>()),
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
                        if (enabledPatchers.Count == 0) return GetResponse<PatcherVm>.Fail("There are no enabled patchers to run.");
                        if (!dataFolder.Succeeded) return dataFolder.BubbleFailure<PatcherVm>();
                        if (!loadOrder.Succeeded) return loadOrder.BubbleFailure<PatcherVm>();
                        if (missingMods.Count > 0)
                        {
                            return GetResponse<PatcherVm>.Fail($"Load order had mods that were missing:{Environment.NewLine}{string.Join(Environment.NewLine, missingMods.Select(x => x.ModKey))}");
                        }
                        if (erroredEnabledPatchers.Count > 0)
                        {
                            var errPatcher = erroredEnabledPatchers.First();
                            return GetResponse<PatcherVm>.Fail(errPatcher, $"\"{errPatcher.NameVm.Name}\" has a blocking error: {errPatcher.State.RunnableState.Reason}");
                        }
                        return GetResponse<PatcherVm>.Succeed(null!);
                    })
                .Throttle(TimeSpan.FromMilliseconds(200), RxApp.MainThreadScheduler)
                .Do(x =>
                {
                    if (x.Failed)
                    {
                        logger.Warning($"Encountered blocking overall error: {x.Reason}");
                    }
                })
                .ToGuiProperty(this, nameof(LargeOverallError), GetResponse<PatcherVm>.Fail("Uninitialized overall error"));

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
                            OverallErrorVm.BackAction = () => DisplayController.SelectedObject = curDisplayed;
                        }
                        else
                        {
                            OverallErrorVm.BackAction = null;
                        }
                        DisplayController.SelectedObject = OverallErrorVm;
                    }
                },
                disposable: this);

            // Forward overall errors into VM
            this.WhenAnyValue(x => x.LargeOverallError)
                .Subscribe(err =>
                {
                    if (err.Succeeded || err.Value != null)
                    {
                        OverallErrorVm.String = null;
                    }
                    else
                    {
                        OverallErrorVm.String = err.Reason;
                    }
                })
                .DisposeWith(this);
            
            _SelectedPatcher = this.WhenAnyValue(x => x.DisplayController.SelectedObject)
                .Select(x => x as PatcherVm)
                .ToGuiProperty(this, nameof(SelectedPatcher), default);

            SetAllToProfileCommand = ReactiveCommand.Create(
                execute: () =>
                {
                    foreach (var patcher in Patchers.Items)
                    {
                        if (patcher is GitPatcherVm gitPatcher)
                        {
                            gitPatcher.NugetTargeting.MutagenVersioning = PatcherNugetVersioningEnum.Profile;
                            gitPatcher.NugetTargeting.SynthesisVersioning = PatcherNugetVersioningEnum.Profile;
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
                .Transform(x => x as GitPatcherVm)
                .ChangeNotNull()
                .Transform(x => CommandVM.Factory(x.UpdateAllCommand.Command))
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
                                logger.Error(ex, "Error updating a patcher");
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
                            logger.Error("Error creating simple link cache for GUI lookups", ex);
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
                    Path.Combine(subDir, _PipelinePaths.Path),
                    JsonConvert.SerializeObject(pipeSettings, Formatting.Indented, Execution.Constants.JsonSettings));
                File.WriteAllText(
                    Path.Combine(subDir, _GuiPaths.Path),
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
                _Logger.Error(ex, "Error while exporting settings");
            }
        }

        public PatchersRunVm GetRun()
        {
            return _RunFactory.GetRun();
        }
    }
}
