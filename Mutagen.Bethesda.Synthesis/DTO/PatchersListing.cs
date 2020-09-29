using System;
using System.Collections.Generic;
using System.Text;

namespace Mutagen.Bethesda.Synthesis.DTO
{
    public class PatchersListing
    {
        public PatcherMeta[] Patchers { get; set; } = Array.Empty<PatcherMeta>();
    }
}
