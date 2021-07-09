using Noggog.WPF;
using Synthesis.Bethesda.DTO;
using System;
using System.Collections.Generic;
using System.Text;

namespace Synthesis.Bethesda.GUI
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
