using Noggog.WPF;
using Synthesis.Bethesda.DTO;

namespace Synthesis.Bethesda.GUI.ViewModels.Patchers.Initialization.Git
{
    public class RepositoryStoreListingVm : ViewModel
    {
        public RepositoryListing Raw { get; }

        public int Stars { get; }

        public int Forks { get; }

        public RepositoryStoreListingVm(RepositoryListing listing)
        {
            Raw = listing;
        }
    }
}
