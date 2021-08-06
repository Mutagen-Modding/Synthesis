using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Synthesis.Bethesda.DTO
{
    [ExcludeFromCodeCoverage]
    public class MutagenPatchersListing
    {
        public RepositoryListing[] Repositories { get; set; } = Array.Empty<RepositoryListing>();
    }
}
