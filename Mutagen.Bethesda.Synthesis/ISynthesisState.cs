using Synthesis.Bethesda;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mutagen.Bethesda.Synthesis
{
    public interface ISynthesisState
    {
        IRunPipelineSettings Settings { get; }
        IModGetter PatchMod { get; }
    }
}
