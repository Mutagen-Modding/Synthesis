using System;
using System.Collections.Generic;
using System.Text;

namespace Synthesis.Bethesda.DTO
{
    public class MutagenPatchersListing
    {
        public RepositoryListing[] Repositories { get; set; } = Array.Empty<RepositoryListing>();
    }
}
