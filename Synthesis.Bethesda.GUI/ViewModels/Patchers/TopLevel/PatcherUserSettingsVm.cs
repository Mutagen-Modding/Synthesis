using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using LibGit2Sharp;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Synthesis.WPF;
using Mutagen.Bethesda.WPF.Plugins.Order;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using Serilog;
using Synthesis.Bethesda.DTO;
using Noggog.GitRepository;
using Synthesis.Bethesda.Execution.PatcherCommands;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Patchers.Git.Services;
using Synthesis.Bethesda.GUI.Services.Patchers.Git;
using Synthesis.Bethesda.GUI.ViewModels.Groups;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.Plugins;

namespace Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel;

public class PatcherUserSettingsVm : ViewModel
{
    public record Inputs(GetResponse<TargetProject> TargetProject, string? SynthVersion, FilePath MetaPath);
    
    private readonly IInitRepository _initRepository;
    private readonly IProvideRepositoryCheckouts _repoCheckouts;

    public ReactiveCommand<Unit, Unit> OpenSettingsCommand { get; }

    private readonly ObservableAsPropertyHelper<SettingsConfiguration> _settingsConfiguration;
    public SettingsConfiguration SettingsConfiguration => _settingsConfiguration.Value;

    private readonly ObservableAsPropertyHelper<bool> _settingsOpen;
    public bool SettingsOpen => _settingsOpen.Value;

    private bool _hasBeenRetrieved = false;
    private readonly ObservableAsPropertyHelper<AutogeneratedSettingsVm?> _reflectionSettings;
    public AutogeneratedSettingsVm? ReflectionSettings
    {
        get
        {
            _hasBeenRetrieved = true;
            return _reflectionSettings.Value;
        }
    }

    public delegate PatcherUserSettingsVm Factory(
        bool needBuild,
        IObservable<Inputs> source);
        
    public PatcherUserSettingsVm(
        ILogger logger,
        IProfileLoadOrder loadOrder,
        IProfileSimpleLinkCacheVm linkCacheVm,
        IObservable<Inputs> source,
        bool needBuild,
        IInitRepository initRepository,
        IProvideAutogeneratedSettings autoGenSettingsProvider,
        IProvideRepositoryCheckouts repoCheckouts,
        IGetSettingsStyle getSettingsStyle,
        IModKeyProvider modKeyProvider,
        IExecuteOpenForSettings executeOpenForSettings,
        IPatcherRunnabilityCliState runnabilityCliState,
        IOpenSettingsHost openSettingsHost)
    {
        _initRepository = initRepository;
        _repoCheckouts = repoCheckouts;
        _settingsConfiguration = source
            .Select(i =>
            {
                return Observable.Create<SettingsConfiguration>(async (observer, cancel) =>
                {
                    observer.OnNext(new SettingsConfiguration(SettingsStyle.None, Array.Empty<ReflectionSettingsConfig>()));
                    if (i.TargetProject.Failed) return;

                    try
                    {
                        var result = await getSettingsStyle.Get(
                            i.TargetProject.Value.ProjPath,
                            directExe: false,
                            cancel: cancel,
                            buildMetaPath: i.MetaPath,
                            build: needBuild).ConfigureAwait(false);
                        logger.Information("Settings type: {Result}", result);
                        observer.OnNext(result);
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "Error checking if patcher can open settings");
                    }
                    observer.OnCompleted();
                });
            })
            .Switch()
            .ToGuiProperty(this, nameof(SettingsConfiguration), new SettingsConfiguration(SettingsStyle.None, Array.Empty<ReflectionSettingsConfig>()));

        OpenSettingsCommand = NoggogCommand.CreateFromObject(
            objectSource: Observable.CombineLatest(
                source.Select(x => x.TargetProject),
                this.WhenAnyValue(x => x.SettingsConfiguration),
                (Proj, Conf) => (Proj, Conf)),
            canExecute: x => x.Proj.Succeeded 
                             && (x.Conf.Style == SettingsStyle.Open || x.Conf.Style == SettingsStyle.Host),
            execute: async (o) =>
            {
                if (o.Conf.Style == SettingsStyle.Open)
                {
                    var modKey = modKeyProvider.ModKey;
                    if (modKey == null)
                    {
                        logger.Information($"Checking runnability failed: No known ModKey");
                        return;
                    }
                    
                    await executeOpenForSettings.Open(
                        o.Proj.Value.ProjPath,
                        directExe: false,
                        modKey: modKey.Value,
                        cancel: CancellationToken.None,
                        loadOrder: loadOrder.LoadOrder.Items.Select<ReadOnlyModListingVM, IModListingGetter>(lvm => lvm)).ConfigureAwait(false);
                }
                else
                {
                    await openSettingsHost.Open(
                        path: o.Proj.Value.ProjPath,
                        cancel: CancellationToken.None,
                        loadOrder: loadOrder.LoadOrder.Items.Select<ReadOnlyModListingVM, IModListingGetter>(lvm => lvm)).ConfigureAwait(false);
                }
                runnabilityCliState.CheckAgain();
            },
            disposable: this);

        _settingsOpen = OpenSettingsCommand.IsExecuting
            .ToGuiProperty(this, nameof(SettingsOpen), deferSubscription: true);

        _reflectionSettings = Observable.CombineLatest(
                this.WhenAnyValue(x => x.SettingsConfiguration),
                source.Select(x => x.TargetProject),
                (SettingsConfig, TargetProject) => (SettingsConfig, TargetProject))
            .Select(x =>
            {
                if (x.TargetProject.Failed
                    || x.SettingsConfig.Style != SettingsStyle.SpecifiedClass
                    || x.SettingsConfig.Targets.Length == 0)
                {
                    return default(AutogeneratedSettingsVm?);
                }
                return autoGenSettingsProvider.Get(x.SettingsConfig,
                    targetProject: x.TargetProject.Value,
                    loadOrder: loadOrder.LoadOrder.Connect().Transform<ReadOnlyModListingVM, IModListingGetter>(x => x),
                    linkCache: linkCacheVm.WhenAnyValue(x => x.SimpleLinkCache));
            })
            .ToGuiProperty<AutogeneratedSettingsVm?>(this, nameof(ReflectionSettings), initialValue: null, deferSubscription: true);
    }

    public void Persist()
    {
        if (!_hasBeenRetrieved) return;
        ReflectionSettings?.Bundle?.Settings?.ForEach(vm =>
        {
            vm.Persist();
            _initRepository.Init(vm.SettingsFolder);
            using var repo = _repoCheckouts.Get(vm.SettingsFolder);
            repo.Repository.Stage(vm.SettingsSubPath);
            try
            {
                repo.Repository.Commit("Settings changed");
            }
            catch (EmptyCommitException)
            {
            }
        });
    }

    public override void Dispose()
    {
        base.Dispose();
        if (_hasBeenRetrieved)
        {
            ReflectionSettings?.Dispose();
        }
    }
}