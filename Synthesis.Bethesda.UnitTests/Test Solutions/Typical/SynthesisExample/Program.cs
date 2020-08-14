using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using System;
using System.Threading.Tasks;

namespace SynthesisExample
{
    class Program : ISkyrimLePatcher, ISkyrimSePatcher
    {
        static void Main(string[] args)
        {
            var program = new Program();
            SynthesisPipeline.Instance.Patch<ISkyrimMod, ISkyrimModGetter>(
                args, 
                program.RunPatch,
                new UserPreferences()
                {
                    ActionsForEmptyArgs = new RunDefaultPatcher()
                    {
                        IdentifyingModKey = "Test.esp",
                        TargetRelease = GameRelease.SkyrimSE,
                    },
                });
        }

        public async Task RunPatch(SynthesisState<ISkyrimMod, ISkyrimModGetter> state)
        {
            //Add a null item entry to all NPCs
            foreach (var npc in state.LoadOrder.PriorityOrder.WinningOverrides<INpcGetter>())
            {
                var patchNpc = state.PatchMod.Npcs.GetOrAddAsOverride(npc);
                patchNpc.Configuration.MagickaOffset += 2;
            }
        }
    }
}
