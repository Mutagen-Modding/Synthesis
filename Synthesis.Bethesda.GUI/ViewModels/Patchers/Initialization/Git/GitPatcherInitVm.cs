using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
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
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.GUI.Services.Main;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Git;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel;
using Synthesis.Bethesda.GUI.ViewModels.Profiles;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.PatcherInstantiation;

namespace Synthesis.Bethesda.GUI.ViewModels.Patchers.Initialization.Git
{
    public class GitPatcherInitVm : ViewModel, IPatcherInitVm
    {
        private readonly IPatcherInitializationVm _init;
        private readonly IPatcherFactory _patcherFactory;
        private readonly PatcherInitRenameValidator _renamer;

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

        [Reactive]
        public bool ShowAll { get; set; }

        private bool _wasAdded = false;

        public GitPatcherInitVm(
            IPatcherInitializationVm init,
            ILogger logger,
            IPatcherFactory patcherFactory,
            INavigateTo navigateTo, 
            PatcherStoreListingVm.Factory listingVmFactory,
            IRegistryListingsProvider listingsProvider,
            PatcherInitRenameValidator renamer)
        {
            _init = init;
            _patcherFactory = patcherFactory;
            _renamer = renamer;
            Patcher = patcherFactory.GetGitPatcher();

            _canCompleteConfiguration = this.WhenAnyValue(x => x.Patcher.RepoClonesValid.Valid)
                .Select(x => ErrorResponse.Create(x))
                .ToGuiProperty(this, nameof(CanCompleteConfiguration), ErrorResponse.Success);

            PatcherRepos = Observable.Return(Unit.Default)
                .ObserveOn(RxApp.TaskpoolScheduler)
                .SelectTask(async _ =>
                {
                    try
                    {
                        var customization = listingsProvider.Get(CancellationToken.None);
                        
                        if (customization.Failed) return Observable.Empty<IChangeSet<PatcherStoreListingVm>>();
                        
                        return customization.Value
                            .SelectMany(repo =>
                            {
                                return repo.Patchers
                                    .Select(p =>
                                    {
                                        return listingVmFactory(this, p, repo);
                                    });
                            })
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
                .Sort(Comparer<PatcherStoreListingVm>.Create((x, y) => x.Name.CompareTo(y.Name)))
                .Filter(this.WhenAnyValue(x => x.ShowAll)
                    .DistinctUntilChanged()
                    .Select(show => new Func<PatcherStoreListingVm, bool>(
                        (p) =>
                        {
                            if (p.Raw.Customization?.Visibility is VisibilityOptions.Visible) return true;
                            else if (p.Raw.Customization?.Visibility is VisibilityOptions.IncludeButHide) return show;
                            else if (p.Raw.Customization?.Visibility is VisibilityOptions.Exclude) return false; // just in case.
                            else return true;
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
                SelectedProjectSubpath = listing.Raw.ProjectPath.Replace('/', '\\')
            });
            if (await _renamer.ConfirmNameUnique(patcher))
            {
                _init.AddNewPatchers(patcher.AsEnumerable<PatcherVm>().ToList());
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
}
