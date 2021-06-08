using DynamicData;
using DynamicData.Binding;
using LibGit2Sharp;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.DTO;
using Synthesis.Bethesda.Execution;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive; 
using System.Reactive.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Synthesis.Bethesda.Execution.CLI;
using Synthesis.Bethesda.Execution.GitRespository;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.GUI.Services;
using Synthesis.Bethesda.GUI.Settings;
using Synthesis.Bethesda.GUI.Temporary;

namespace Synthesis.Bethesda.GUI
{
    public class GitPatcherInitVM : PatcherInitVM
    {
        public ProfileVM Profile { get; }
        private readonly ObservableAsPropertyHelper<ErrorResponse> _CanCompleteConfiguration;
        public override ErrorResponse CanCompleteConfiguration => _CanCompleteConfiguration.Value;

        public GitPatcherVM Patcher { get; }

        [Reactive]
        public string Search { get; set; } = string.Empty;

        public IObservableCollection<PatcherStoreListingVM> PatcherRepos { get; }

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
        public PatcherStoreListingVM? SelectedPatcher { get; set; }

        [Reactive]
        public bool ShowAll { get; set; }

        private bool _wasAdded = false;

        public GitPatcherInitVM(
            PatcherInitializationVM init,
            ProfileVM profile, 
            INavigateTo navigateTo, 
            IProvideRepositoryCheckouts repositoryCheckouts,
            ICheckOrCloneRepo checkOrClone)
            : base(init)
        {
            Profile = profile;
            Patcher = new GitPatcherVM(profile, 
                profile.Scope.GetRequiredService<IRemovePatcherFromProfile>(),
                navigateTo, checkOrClone,
                profile.Scope.GetRequiredService<IProvideRepositoryCheckouts>(),
                profile.Scope.GetRequiredService<ICheckoutRunnerRepository>(),
                profile.Scope.GetRequiredService<ICheckRunnability>(),
                profile.Scope.GetInstance<IProfileDisplayControllerVm>(),
                profile.Scope.GetInstance<IConfirmationPanelControllerVm>(),
                profile.Scope.GetInstance<ILockToCurrentVersioning>(),
                profile.Scope.GetRequiredService<IBuild>());

            _CanCompleteConfiguration = this.WhenAnyValue(x => x.Patcher.RepoClonesValid)
                .Select(x => ErrorResponse.Create(x))
                .ToGuiProperty(this, nameof(CanCompleteConfiguration), ErrorResponse.Success);

            PatcherRepos = Observable.Return(Unit.Default)
                .ObserveOn(RxApp.TaskpoolScheduler)
                .SelectTask(async _ =>
                {
                    try
                    {
                        var localRepoPath = await checkOrClone.Check(
                            GetResponse<string>.Succeed("https://github.com/Mutagen-Modding/Synthesis.Registry"),
                            Paths.RegistryFolder,
                            Log.Logger.Error,
                            CancellationToken.None);
                        if (localRepoPath.Failed)
                        {
                            Error = localRepoPath;
                            return Observable.Empty<IChangeSet<PatcherStoreListingVM>>();
                        }
                        using var repoCheckout = repositoryCheckouts.Get(localRepoPath.Value.Local);
                        var repo = repoCheckout.Repository;
                        repo.Fetch();

                        var master = repo.Branches.Where(b => b.IsCurrentRepositoryHead).FirstOrDefault();
                        if (master == null)
                        {
                            Error = ErrorResponse.Fail("Could not find master branch");
                            Log.Logger.Error(Error.Reason);
                            return Observable.Empty<IChangeSet<PatcherStoreListingVM>>();
                        }
                        repo.Reset(ResetMode.Hard, repo.Branches[$"{master.RemoteName}/{master.FriendlyName}"].Tip, new CheckoutOptions());

                        var listingPath = Path.Combine(repo.Info.WorkingDirectory, Constants.AutomaticListingFileName);
                        if (!File.Exists(listingPath))
                        {
                            Error = ErrorResponse.Fail("Could not locate listing file");
                            Log.Logger.Error(Error.Reason);
                            return Observable.Empty<IChangeSet<PatcherStoreListingVM>>();
                        }
                        var settings = new JsonSerializerOptions();
                        settings.Converters.Add(new JsonStringEnumConverter());
                        var customization = JsonSerializer.Deserialize<MutagenPatchersListing>(File.ReadAllText(listingPath), settings)!;
                        return customization.Repositories
                            .NotNull()
                            .SelectMany(repo =>
                            {
                                var repoVM = new RepositoryStoreListingVM(repo);
                                return repo.Patchers
                                    .Select(p =>
                                    {
                                        return new PatcherStoreListingVM(this, p, repoVM, Inject.Scope.GetRequiredService<INavigateTo>());
                                    });
                            })
                            .AsObservableChangeSet();
                    }
                    catch (Exception ex)
                    {
                        Log.Logger.Error(ex, "Error downloading patcher listing");
                        Error = ErrorResponse.Fail(ex);
                    }
                    return Observable.Empty<IChangeSet<PatcherStoreListingVM>>();
                })
                .Switch()
                .Sort(Comparer<PatcherStoreListingVM>.Create((x, y) => x.Name.CompareTo(y.Name)))
                .Filter(this.WhenAnyValue(x => x.ShowAll)
                    .DistinctUntilChanged()
                    .Select(show => new Func<PatcherStoreListingVM, bool>(
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
                            return new Func<PatcherStoreListingVM, bool>(_ => true);
                        }
                        return new Func<PatcherStoreListingVM, bool>(
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

        public override async IAsyncEnumerable<PatcherVM> Construct()
        {
            _wasAdded = true;
            yield return Patcher;
        }

        public override void Cancel()
        {
            base.Cancel();
            Patcher.Delete();
        }

        public void AddStorePatcher(PatcherStoreListingVM listing)
        {
            PatcherVM patcher = new GitPatcherVM(
                Profile,
                Profile.Scope.GetRequiredService<IRemovePatcherFromProfile>(),
                Profile.Scope.GetRequiredService<INavigateTo>(),
                Profile.Scope.GetRequiredService<ICheckOrCloneRepo>(),
                Profile.Scope.GetRequiredService<IProvideRepositoryCheckouts>(),
                Profile.Scope.GetRequiredService<ICheckoutRunnerRepository>(),
                Profile.Scope.GetRequiredService<ICheckRunnability>(),
                Profile.Scope.GetInstance<IProfileDisplayControllerVm>(),
                Profile.Scope.GetInstance<IConfirmationPanelControllerVm>(),
                Profile.Scope.GetInstance<ILockToCurrentVersioning>(),
                Profile.Scope.GetRequiredService<IBuild>())
            {
                RemoteRepoPath = listing.RepoPath,
                ProjectSubpath = listing.Raw.ProjectPath.Replace('/', '\\')
            };
            Init.AddNewPatchers(patcher.AsEnumerable().ToList());
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
