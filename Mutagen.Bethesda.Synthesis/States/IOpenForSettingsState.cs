using Synthesis.Bethesda;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mutagen.Bethesda.Synthesis
{
    public interface IOpenForSettingsState
    {
        OpenForSettings Settings { get; }
        IEnumerable<LoadOrderListing> LoadOrder { get; }
    }
}
