using Mutagen.Bethesda.Skyrim;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Mutagen.Bethesda.Synthesis;

public interface ISkyrimLePatcher : IPatcher
{
    Task RunPatch(SynthesisState<ISkyrimMod, ISkyrimModGetter> state);
}

public interface ISkyrimSePatcher : IPatcher
{
    Task RunPatch(SynthesisState<ISkyrimMod, ISkyrimModGetter> state);
}