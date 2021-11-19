using System;
using System.IO;
using System.Reactive.Linq;
using System.Windows.Input;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using Synthesis.Bethesda.DTO;
using Synthesis.Bethesda.GUI.Services.Main;

namespace Synthesis.Bethesda.GUI.ViewModels.Patchers.Initialization.Git
{
    public class PatcherStoreListingVm : ViewModel
    {
        public RepositoryStoreListingVm Repository { get; }
        public PatcherListing Raw { get; }
        public string Name { get; }

        private readonly ObservableAsPropertyHelper<bool> _IsSelected;
        public bool IsSelected => _IsSelected.Value;

        public ICommand OpenWebsite { get; }
        public ICommand AddCommand { get; }

        public string RepoPath => $"https://github.com/{Repository.Raw.User}/{Repository.Raw.Repository}";

        public delegate PatcherStoreListingVm Factory(
            GitPatcherInitVm gitInit,
            PatcherListing listing,
            RepositoryListing repositoryListing);
        
        public PatcherStoreListingVm(
            GitPatcherInitVm gitInit,
            PatcherListing listing,
            RepositoryListing repositoryListing,
            INavigateTo navigate)
        {
            Repository = new RepositoryStoreListingVm(repositoryListing);
            Raw = listing;
            try
            {
                Name = Raw.Customization?.Nickname ?? Path.GetFileName(Raw.ProjectPath).TrimEnd(".csproj");
            }
            catch (Exception)
            {
                Name = "Error";
            }
            _IsSelected = gitInit.WhenAnyValue(x => x.SelectedPatcher)
                .Select(x => x == this)
                .ToGuiProperty(this, nameof(IsSelected));
            OpenWebsite = ReactiveCommand.Create(() => navigate.Navigate(RepoPath));
            AddCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                await gitInit.AddStorePatcher(this);
            });
        }

        public override string ToString()
        {
            return $"{Repository.Raw.User}/{Repository.Raw.Repository}/{Raw.ProjectPath}";
        }
    }
}
