using Noggog;
using Noggog.WPF;
using ReactiveUI;
using Synthesis.Bethesda.DTO;
using System;
using System.Diagnostics;
using System.IO;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Synthesis.Bethesda.GUI
{
    public class PatcherStoreListingVM : ViewModel
    {
        public RepositoryStoreListingVM Repository { get; }
        public PatcherListing Raw { get; }
        public string Name { get; }

        private readonly ObservableAsPropertyHelper<bool> _IsSelected;
        public bool IsSelected => _IsSelected.Value;

        public ICommand OpenWebsite { get; }
        public ICommand AddCommand { get; }

        public string RepoPath => $"https://github.com/{Repository.Raw.User}/{Repository.Raw.Repository}";

        public PatcherStoreListingVM(GitPatcherInitVM gitInit, PatcherListing listing, RepositoryStoreListingVM repo)
        {
            Repository = repo;
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
            OpenWebsite = ReactiveCommand.Create(() => Utility.OpenWebsite(RepoPath));
            AddCommand = ReactiveCommand.Create(() =>
            {
                gitInit.AddStorePatcher(this);
            });
        }

        public override string ToString()
        {
            return $"{Repository.Raw.User}/{Repository.Raw.Repository}/{Raw.ProjectPath}";
        }
    }
}
