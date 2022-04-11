using Mutagen.Bethesda.Oblivion;
using System.Threading.Tasks;

namespace Mutagen.Bethesda.Synthesis;

public interface IOblivionPatcher : IPatcher
{
    Task RunPatch(SynthesisState<IOblivionMod, IOblivionModGetter> state);
}