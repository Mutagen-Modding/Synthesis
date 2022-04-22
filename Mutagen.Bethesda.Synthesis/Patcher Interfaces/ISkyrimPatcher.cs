using Mutagen.Bethesda.Skyrim;

namespace Mutagen.Bethesda.Synthesis;

public interface ISkyrimLePatcher : IPatcher
{
    Task RunPatch(SynthesisState<ISkyrimMod, ISkyrimModGetter> state);
}

public interface ISkyrimSePatcher : IPatcher
{
    Task RunPatch(SynthesisState<ISkyrimMod, ISkyrimModGetter> state);
}