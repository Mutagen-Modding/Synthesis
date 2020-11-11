using DynamicData;
using DynamicData.Binding;
using Noggog;
using Noggog.WPF;
using Octokit;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.DTO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text.Json;
using System.Windows.Input;

namespace Synthesis.Bethesda.GUI
{
    public class GitPatcherInitVM : PatcherInitVM
    {
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

        public GitPatcherInitVM(ProfileVM profile)
            : base(profile)
        {
            Patcher = new GitPatcherVM(profile);
            this.CompositeDisposable.Add(Patcher);

            _CanCompleteConfiguration = this.WhenAnyValue(x => x.Patcher.State)
                .Select(x => x.RunnableState)
                .ToGuiProperty(this, nameof(CanCompleteConfiguration), ErrorResponse.Success);

            PatcherRepos = Observable.Return(Unit.Default)
                .ObserveOn(RxApp.TaskpoolScheduler)
                .SelectTask(async _ =>
                {
                    try
                    {
                        var gitHubClient = new GitHubClient(new ProductHeaderValue("Synthesis"));
                        gitHubClient.Credentials = new Credentials("9b58542a2ca303d7ced129cc404191f8eea519f7");
                        var content = await gitHubClient.Repository.Content.GetAllContents("Noggog", "Synthesis.Registry", Constants.AutomaticListingFileName);
                        if (content.Count != 1) return Observable.Empty<IChangeSet<PatcherStoreListingVM>>();
                        var customization = JsonSerializer.Deserialize<MutagenPatchersListing>(content[0].Content);
                        return customization.Repositories
                            .NotNull()
                            .SelectMany(repo =>
                            {
                                var repoVM = new RepositoryStoreListingVM(repo);
                                return repo.Patchers
                                    .Select(p =>
                                    {
                                        return new PatcherStoreListingVM(this, p, repoVM);
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

            OpenPopulationInfoCommand = ReactiveCommand.Create(() => Utility.NavigateToPath(Constants.ListingRepositoryAddress));
            ClearSearchCommand = ReactiveCommand.Create(() => Search = string.Empty);
        }

        public override async IAsyncEnumerable<PatcherVM> Construct()
        {
            yield return Patcher;
        }

        public override void Cancel()
        {
            base.Cancel();
            Patcher.Delete();
        }

        public void AddStorePatcher(PatcherStoreListingVM listing)
        {
            PatcherVM patcher = new GitPatcherVM(Profile)
            {
                RemoteRepoPath = listing.RepoPath,
                ProjectSubpath = listing.Raw.ProjectPath.Replace('/', '\\')
            };
            Profile.Config.AddNewPatchers(patcher.AsEnumerable().ToList());
        }
    }
}
