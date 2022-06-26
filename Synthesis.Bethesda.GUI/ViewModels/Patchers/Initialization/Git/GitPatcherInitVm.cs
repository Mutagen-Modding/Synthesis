using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Input;
using DynamicData;
using DynamicData.Binding;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Synthesis.Bethesda.DTO;
using Synthesis.Bethesda.Execution.Patchers.Git.Registry;
using Synthesis.Bethesda.Execution.Pathing;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.GUI.Services.Main;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Git;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel;
using Synthesis.Bethesda.GUI.ViewModels.Profiles;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.PatcherInstantiation;
using Synthesis.Bethesda.GUI.ViewModels.Top;

namespace Synthesis.Bethesda.GUI.ViewModels.Patchers.Initialization.Git;

public class GitPatcherInitVm : ViewModel, IPatcherInitVm
{
    public InitializationSettingsVm InitializationSettingsVm { get; }
    private readonly IPatcherInitializationVm _init;
    private readonly IAddPatchersToSelectedGroupVm _addNewPatchers;
    private readonly IPatcherFactory _patcherFactory;
    private readonly PatcherInitRenameValidator _renamer;
    private readonly IPathSanitation _pathSanitation;
    private readonly IProfileDisplayControllerVm _displayControllerVm;

    public ICommand CompleteConfiguration => _init.CompleteConfiguration;
    public ICommand CancelConfiguration => _init.CancelConfiguration;
        
    private readonly ObservableAsPropertyHelper<ErrorResponse> _canCompleteConfiguration;
    public ErrorResponse CanCompleteConfiguration => _canCompleteConfiguration.Value;

    public GitPatcherVm Patcher { get; }

    [Reactive]
    public string Search { get; set; } = string.Empty;

    public IObservableCollection<PatcherStoreListingVm> PatcherRepos { get; }

    [Reactive]
    public ErrorResponse Error { get; private set; } = ErrorResponse.Success;

    public enum TabType
    {
        Browse,
        Input,
    }

    [Reactive]
    public TabType SelectedTab { get; set; } = TabType.Browse;

    public ICommand OpenPopulationInfoCommand { get; }
    public ICommand ClearSearchCommand { get; }

    [Reactive]
    public PatcherStoreListingVm? SelectedPatcher { get; set; }

    private bool _wasAdded = false;

    public GitPatcherInitVm(
        IPatcherInitializationVm init,
        IAddPatchersToSelectedGroupVm addNewPatchers,
        ILogger logger,
        IPatcherFactory patcherFactory,
        INavigateTo navigateTo, 
        IPathSanitation pathSanitation,
        PatcherStoreListingVm.Factory listingVmFactory,
        IProfileDisplayControllerVm displayControllerVm,
        ApplicablePatcherListingsProvider listingsProvider,
        IProfileGroupsList groups,
        InitializationSettingsVm initializationSettingsVm,
        PatcherInitRenameValidator renamer)
    {
        InitializationSettingsVm = initializationSettingsVm;
        _init = init;
        _addNewPatchers = addNewPatchers;
        _patcherFactory = patcherFactory;
        _renamer = renamer;
        _pathSanitation = pathSanitation;
        _displayControllerVm = displayControllerVm;
        Patcher = patcherFactory.GetGitPatcher();

        _canCompleteConfiguration = this.WhenAnyValue(x => x.Patcher.RepoClonesValid.Valid)
            .Select(x => ErrorResponse.Create(x))
            .ToGuiProperty(this, nameof(CanCompleteConfiguration), ErrorResponse.Success);

        var installedPatcherPaths =
            groups.Groups.Items
                .SelectMany(g => g.Patchers.Items)
                .WhereCastable<PatcherVm, GitPatcherVm>()
                .Select(p => p.RemoteRepoPathInput.RemoteRepoPath)
                .Distinct()
                .ToHashSet();

        PatcherRepos = Observable.Return(Unit.Default)
            .ObserveOn(RxApp.TaskpoolScheduler)
            .SelectTask(async _ =>
            {
                try
                {
                    var customization = listingsProvider.Get(CancellationToken.None);
                        
                    if (customization.Failed) return Observable.Empty<IChangeSet<PatcherStoreListingVm>>();
                        
                    return customization.Value
                        .Select(x => listingVmFactory(this, x.Patcher, x.Repository))
                        .AsObservableChangeSet();
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Error downloading patcher listing");
                    Error = ErrorResponse.Fail(ex);
                }
                return Observable.Empty<IChangeSet<PatcherStoreListingVm>>();
            })
            .Switch()
            .Filter(this.WhenAnyValue(x => x.InitializationSettingsVm.ShowUnlisted)
                .DistinctUntilChanged()
                .Select(show => new Func<PatcherStoreListingVm, bool>(
                    (p) =>
                    {
                        return p.Raw.Customization?.Visibility switch
                        {
                            VisibilityOptions.Exclude => false,
                            VisibilityOptions.IncludeButHide => show,
                            VisibilityOptions.Visible => true,
                            _ => true
                        };
                    })))
            .Filter(
                this.WhenAnyValue(x => x.InitializationSettingsVm.ShowInstalled)
                    .DistinctUntilChanged()
                .Select(show => new Func<PatcherStoreListingVm, bool>(
                    (p) =>
                    {
                        if (show) return true;
                        return !installedPatcherPaths.Contains(p.RepoPath);
                    })))
            .Filter(this.WhenAnyValue(x => x.Search)
                .Debounce(TimeSpan.FromMilliseconds(350), RxApp.MainThreadScheduler)
                .Select(x => x.Trim())
                .DistinctUntilChanged()
                .Select(search =>
                {
                    if (string.IsNullOrWhiteSpace(search))
                    {
                        return new Func<PatcherStoreListingVm, bool>(_ => true);
                    }
                    return new Func<PatcherStoreListingVm, bool>(
                        (p) =>
                        {
                            if (p.Name.Contains(search, StringComparison.OrdinalIgnoreCase)) return true;
                            if (p.Raw.Customization?.OneLineDescription?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) return true;
                            return false;
                        });
                }))
            .Sort(Comparer<PatcherStoreListingVm>.Create((x, y) => x.Name.CompareTo(y.Name)))
            .ObserveOn(RxApp.MainThreadScheduler)
            .ToObservableCollection(this);

        OpenPopulationInfoCommand = ReactiveCommand.Create(() => navigateTo.Navigate(Constants.ListingRepositoryAddress));
        ClearSearchCommand = ReactiveCommand.Create(() => Search = string.Empty);
    }

    public async IAsyncEnumerable<PatcherVm> Construct()
    {
        _wasAdded = true;
        yield return Patcher;
    }

    public void Cancel()
    {
        Patcher.Delete();
    }
        
    public async Task AddStorePatcher(PatcherStoreListingVm listing)
    {
        var patcher = _patcherFactory.GetGitPatcher(new GithubPatcherSettings()
        {
            RemoteRepoPath = listing.RepoPath,
            SelectedProjectSubpath = _pathSanitation.Sanitize(listing.Raw.ProjectPath)
        });
        if (_addNewPatchers.CanAddPatchers && await _renamer.ConfirmNameUnique(patcher))
        {
            _init.NewPatcher = null;
            _addNewPatchers.AddNewPatchers(patcher);
            _displayControllerVm.SelectedObject = patcher;
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        if (!_wasAdded)
        {
            Patcher.Dispose();
        }
    }
}