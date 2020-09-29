using System;
using System.Collections.Generic;
using System.Text;

namespace Mutagen.Bethesda.Synthesis.DTO
{
    public class PatchersListing
    {
        public PatcherInfo[] Patchers { get; set; } = Array.Empty<PatcherInfo>();
    }
}
