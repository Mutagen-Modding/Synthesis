using Mutagen.Bethesda.Oblivion;

namespace Mutagen.Bethesda.Synthesis;

public interface IOblivionPatcher : IPatcher
{
    Task RunPatch(SynthesisState<IOblivionMod, IOblivionModGetter> state);
}