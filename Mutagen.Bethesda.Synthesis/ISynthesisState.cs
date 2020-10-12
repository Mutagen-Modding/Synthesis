using Mutagen.Bethesda.Synthesis.CLI;
using Synthesis.Bethesda;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mutagen.Bethesda.Synthesis
{
    public interface ISynthesisState : IDisposable
    {
        RunSynthesisMutagenPatcher Settings { get; }
        IModGetter PatchMod { get; }
        IEnumerable<ModKey> LoadOrder { get; }
    }
}
