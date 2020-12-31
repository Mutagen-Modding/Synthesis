using Synthesis.Bethesda;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mutagen.Bethesda.Synthesis
{
    public interface ISynthesisRunnabilityState
    {
        CheckRunnability Settings { get; }
        IReadOnlyList<LoadOrderListing> RawLoadOrder { get; }
    }
}
