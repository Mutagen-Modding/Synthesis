using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using Autofac;
using DynamicData;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments.DI;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Strings;
using Mutagen.Bethesda.WPF.Plugins.Order;
using Noggog;
using Noggog.WPF;
using Noggog.WPF.Containers;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Synthesis.Bethesda.Execution.Profile;
using Synthesis.Bethesda.Execution.Profile.Services;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.Execution.Settings.V2;
using Synthesis.Bethesda.GUI.Services.Profile.Exporter;
using Synthesis.Bethesda.GUI.Services.Profile.TopLevel;
using Synthesis.Bethesda.GUI.Settings;
using Synthesis.Bethesda.GUI.ViewModels.Groups;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Git;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.Plugins;
using Synthesis.Bethesda.GUI.ViewModels.Top;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles;

public class ProfileVm : ViewModel
{
    private readonly StartRun _startRun;
    private readonly ILogger _logger;

    public GameRelease Release { get; }

    public SourceList<GroupVm> Groups { get; }

    public SourceListUiFunnel<GroupVm> GroupsDisplay { get; }

    public ICommand GoToErrorCommand { get; }
    public ICommand EnableAllGroupsCommand { get; }
    public ICommand DisableAllGroupsCommand { get; }
    public ICommand CollapseAllGroupsCommand { get; }
    public ICommand ExpandAllGroupsCommand { get; }
    public ICommand ExportCommand { get; }
    public ReactiveCommand<Unit, Unit> UpdateAllPatchersCommand { get; }

    public string ID { get; private set; }

    public string ProfileDirectory { get; }
    public string WorkingDirectory { get; }

    public IProfileOverridesVm Overrides { get; }
    public IProfileVersioning Versioning { get; }

    private readonly ObservableAsPropertyHelper<DirectoryPath> _dataFolder;
    public DirectoryPath DataFolder => _dataFolder.Value;

    private readonly ObservableAsPropertyHelper<ErrorResponse> _state;
    public ErrorResponse State => _state.Value;

    private readonly ObservableAsPropertyHelper<GetResponse<ViewModel>> _globalError;
    public GetResponse<ViewModel> GlobalError => _globalError.Value;

    private readonly ObservableAsPropertyHelper<GetResponse<ViewModel>> _blockingError;
    public GetResponse<ViewModel> BlockingError => _blockingError.Value;

    public IObservableList<ReadOnlyModListingVM> LoadOrder { get; }

    private readonly ObservableAsPropertyHelper<bool> _isActive;
    public bool IsActive => _isActive.Value;

    public ICommand SetAllToProfileCommand { get; }

    private readonly ObservableAsPropertyHelper<PatcherVm?> _selectedPatcher;
    public PatcherVm? SelectedPatcher => _selectedPatcher.Value;

    [Reactive]
    public bool ConsiderPrereleaseNugets { get; set; }

    public IProfileDisplayControllerVm DisplayController { get; }

    public OverallErrorVm OverallErrorVm { get; }

    public IObservable<ILinkCache?> SimpleLinkCache { get; }

    public ILockToCurrentVersioning LockSetting { get; }
    public IProfileExporter Exporter { get; }

    [Reactive]
    public PersistenceMode SelectedPersistenceMode { get; set; } = PersistenceMode.None;

    public ILifetimeScope Scope { get; }
    public IPatcherInitializationVm Init { get; }
        
    public IProfileNameVm NameVm { get; }

    [Reactive]
    public bool IgnoreMissingMods { get; set; }
        
    [Reactive]
    public bool Localize { get; set; }
        
    [Reactive]
    public Language TargetLanguage { get; set; }
        
    [Reactive]
    public bool MasterFile { get; set; }
        
    [Reactive]
    public bool MasterStyleFallbackEnabled { get; set; }

    [Reactive]
    public bool UseUtf8InEmbedded { get; set; }

    [Reactive]
    public FormIDRangeMode FormIDRangeMode { get; set; } = FormIDRangeMode.Auto;

    [Reactive]
    public float? HeaderVersionOverride { get; set; }

    public IEnvironmentErrorsVm EnvironmentErrors { get; }
    
    [Reactive]
    public MasterStyle MasterStyle { get; set; }

    public ProfileVm(
        ILifetimeScope scope,
        IPatcherInitializationVm initVm,
        IProfileOverridesVm overrides,
        IProfileIdentifier ident,
        IProfileNameVm nameProvider,
        IProfileLoadOrder loadOrder,
        IProfileDirectories dirs,
        IProfileVersioning versioning,
        IProfileDisplayControllerVm profileDisplay,
        ILockToCurrentVersioning lockSetting,
        ISelectedProfileControllerVm selProfile,
        IProfileExporter exporter,
        IProfileGroupsList groupsList,
        IEnvironmentErrorsVm environmentErrors,
        OverallErrorVm overallErrorVm,
        StartRun startRun,
        IGameReleaseContext gameReleaseContext,
        AddGitPatcherResponder addGitPatcherResponder,
        ILogger logger)
    {
        Scope = scope;
        Init = initVm;
        OverallErrorVm = overallErrorVm;
        NameVm = nameProvider;
        Groups = groupsList.Groups;
        Overrides = overrides;
        Versioning = versioning;
        LockSetting = lockSetting;
        Exporter = exporter;
        DisplayController = profileDisplay;
        _startRun = startRun;
        _logger = logger;
        ID = ident.ID;
        Release = gameReleaseContext.Release;

        GroupsDisplay = new SourceListUiFunnel<GroupVm>(Groups, this);

        ProfileDirectory = dirs.ProfileDirectory;
        WorkingDirectory = dirs.WorkingDirectory;
            
        EnvironmentErrors = environmentErrors;

        _dataFolder = overrides.WhenAnyValue(x => x.DataFolderResult.Value)
            .ToGuiProperty<DirectoryPath>(this, nameof(DataFolder), string.Empty, deferSubscription: true);

        LoadOrder = loadOrder.LoadOrder;

        var enabledGroups = Groups.Connect()
            .ObserveOnGui()
            .FilterOnObservable(p => p.WhenAnyValue(x => x.IsOn), scheduler: RxApp.MainThreadScheduler)
            .RefCount();

        var enabledGroupModKeys = enabledGroups
            .Transform(x => x.ModKey)
            .QueryWhenChanged(q => q.ToHashSet())
            .Replay(1).RefCount();

        _globalError = Observable.CombineLatest(
                overrides.WhenAnyValue(x => x.DataFolderResult),
                loadOrder.WhenAnyValue(x => x.State),
                LoadOrder.Connect()
                    .FilterOnObservable(
                        x =>
                        {
                            return Observable.CombineLatest(
                                x.WhenAnyValue(y => y.ExistsOnDisk)
                                    .DistinctUntilChanged(),
                                enabledGroupModKeys
                                    .Select(groupModKeys => groupModKeys.Contains(x.ModKey)),
                                (exists, isEnabledGroupKey) => !exists && !isEnabledGroupKey);
                        },
                        scheduler: RxApp.MainThreadScheduler)
                    .QueryWhenChanged(q => q)
                    .StartWith(Array.Empty<ReadOnlyModListingVM>())
                    .Throttle(TimeSpan.FromMilliseconds(200), RxApp.MainThreadScheduler),
                this.WhenAnyValue(x => x.IgnoreMissingMods),
                (dataFolder, loadOrder, missingMods, ignoreMissingMods) =>
                {
                    if (!dataFolder.Succeeded) return GetResponse<ViewModel>.Fail(reason: $"DataFolder: {dataFolder.Reason}");
                    if (!loadOrder.Succeeded) return GetResponse<ViewModel>.Fail(reason: $"LoadOrder: {dataFolder.Reason}");
                    if (!ignoreMissingMods && missingMods.Count > 0)
                    {
                        return GetResponse<ViewModel>.Fail(
                            $"Load order had mods that were missing:{Environment.NewLine}{string.Join(Environment.NewLine, missingMods.Select(x => x.ModKey))}");
                    }

                    return GetResponse<ViewModel>.Succeed(null!);
                })
            .ToGuiProperty(this, nameof(GlobalError), GetResponse<ViewModel>.Fail("Uninitialized global error"), deferSubscription: true);

        _blockingError = Observable.CombineLatest(
                this.WhenAnyValue(x => x.GlobalError),
                enabledGroups
                    .QueryWhenChanged(q => q)
                    .StartWith(Array.Empty<GroupVm>()),
                enabledGroups
                    .FilterOnObservable(g => g.WhenAnyValue(x => x.State).Select(x => x.IsHaltingError))
                    .QueryWhenChanged(q => q)
                    .StartWith(Array.Empty<GroupVm>()),
                (global, enabledGroups, erroredEnabledGroups) =>
                {
                    if (enabledGroups.Count == 0) return GetResponse<ViewModel>.Fail("There are no enabled groups to run.");
                    if (erroredEnabledGroups.Count > 0)
                    {
                        var errGroup = erroredEnabledGroups.First();
                        return GetResponse<ViewModel>.Fail(errGroup.State.Item, $"\"{errGroup.Name}\" has a blocking error: {errGroup.State}");
                    }
                    return GetResponse<ViewModel>.Succeed(null!);
                })
            .Throttle(TimeSpan.FromMilliseconds(200), RxApp.MainThreadScheduler)
            .Do(x =>
            {
                if (x.Failed)
                {
                    logger.Warning("Encountered blocking overall error: {Reason}", x.Reason);
                }
                else
                {
                    logger.Information("No global error");
                }
            })
            .ToGuiProperty(this, nameof(BlockingError), GetResponse<ViewModel>.Fail("Uninitialized blocking error"), deferSubscription: true);
            
        _state = Observable.CombineLatest(
                this.WhenAnyValue(x => x.BlockingError),
                Groups.Connect()
                    .ObserveOnGui()
                    .AutoRefresh(x => x.IsOn, scheduler: RxApp.MainThreadScheduler)
                    .Filter(p => p.IsOn)
                    .AutoRefresh(x => x.State, scheduler: RxApp.MainThreadScheduler)
                    .Transform(p => p.State, transformOnRefresh: true)
                    .QueryWhenChanged(errs =>
                    {
                        var errored = errs.FirstOrDefault(e => !e?.RunnableState.Succeeded ?? false);
                        if (errored == null) return ErrorResponse.Success;
                        return errored.RunnableState;
                    }),
                (overall, patcherState) =>
                {
                    if (!overall.Succeeded) return overall;
                    return patcherState;
                })
            .ToGuiProperty<ErrorResponse>(this, nameof(State), ErrorResponse.Fail("Uninitialized state error"), deferSubscription: true);

        _isActive = selProfile.WhenAnyValue(x => x.SelectedProfile)
            .Select(x => x == this)
            .ToGuiProperty(this, nameof(IsActive), deferSubscription: true);

        GoToErrorCommand = OverallErrorVm.CreateCommand(this.WhenAnyValue(x => x.BlockingError));

        // Forward overall errors into VM
        this.WhenAnyValue(x => x.BlockingError)
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
            
        _selectedPatcher = this.WhenAnyValue(x => x.DisplayController.SelectedObject)
            .Select(x => x as PatcherVm)
            .ToGuiProperty(this, nameof(SelectedPatcher), default, deferSubscription: true);

        SetAllToProfileCommand = ReactiveCommand.Create(
            execute: () =>
            {
                foreach (var patcher in Groups.Items.SelectMany(x => x.Patchers.Items))
                {
                    if (patcher is GitPatcherVm gitPatcher)
                    {
                        gitPatcher.NugetTargeting.MutagenVersioning = PatcherNugetVersioningEnum.Profile;
                        gitPatcher.NugetTargeting.SynthesisVersioning = PatcherNugetVersioningEnum.Profile;
                    }
                }
            });

        EnableAllGroupsCommand = ReactiveCommand.Create(() =>
        {
            foreach (var group in Groups.Items)
            {
                group.IsOn = true;
            }
        });
        DisableAllGroupsCommand = ReactiveCommand.Create(() =>
        {
            foreach (var group in Groups.Items)
            {
                group.IsOn = false;
            }
        });
        ExpandAllGroupsCommand = ReactiveCommand.Create(() =>
        {
            foreach (var group in Groups.Items)
            {
                group.Expanded = true;
            }
        });
        CollapseAllGroupsCommand = ReactiveCommand.Create(() =>
        {
            foreach (var group in Groups.Items)
            {
                group.Expanded = false;
            }
        });

        var allCommands = Groups.Connect()
            .ObserveOnGui()
            .Transform(x => CommandVM.Factory(x.UpdateAllPatchersCommand))
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
                            logger.Error(ex, "Error updating a group");
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
                            x.rel,
                            factory: (modPath) => ModInstantiator.Importer(modPath, x.rel));
                        obs.OnNext(loadOrder.ToUntypedImmutableLinkCache(LinkCachePreferences.OnlyIdentifiers()));
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "Error creating simple link cache for GUI lookups");
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

        addGitPatcherResponder.Connect()
            .FlowSwitch(this.WhenAnyValue(x => x.IsActive))
            .Subscribe()
            .DisposeWith(this);
    }
        
    public SynthesisProfile Save()
    {
        return new SynthesisProfile()
        {
            Groups = Groups.Items.Select(p => p.Save()).ToList(),
            ID = ID,
            Nickname = NameVm.Name,
            TargetRelease = Release,
            MutagenManualVersion = Versioning.ManualMutagenVersion,
            SynthesisManualVersion = Versioning.ManualSynthesisVersion,
            MutagenVersioning = Versioning.MutagenVersioning,
            SynthesisVersioning = Versioning.SynthesisVersioning,
            DataPathOverride = Overrides.DataPathOverride,
            ConsiderPrereleaseNugets = ConsiderPrereleaseNugets,
            LockToCurrentVersioning = LockSetting.Lock,
            FormIdPersistence = SelectedPersistenceMode,
            IgnoreMissingMods = IgnoreMissingMods,
            Localize = Localize,
            ExportAsMasterFiles = MasterFile,
            MasterStyleFallbackEnabled = MasterStyleFallbackEnabled,
            MasterStyle = MasterStyle,
            TargetLanguage = TargetLanguage,
            UseUtf8ForEmbeddedStrings = UseUtf8InEmbedded,
            FormIDRangeMode = FormIDRangeMode,
            HeaderVersionOverride = HeaderVersionOverride
        };
    }

    public override void Dispose()
    {
        base.Dispose();
        foreach (var patcher in Groups.Items)
        {
            patcher.Dispose();
        }
    }

    public void Export()
    {
        try
        {
            Exporter.Export(ID);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error while exporting settings");
        }
    }

    public void StartRun()
    {
        _startRun.Start(Groups.Items.Where(x => x.IsOn).ToArray());
    }
}