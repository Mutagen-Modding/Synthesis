# Coding a Patcher
## Starting Setup
If you used a Local [Solution Patcher](../Local-Solution-Patcher.md) to [Create a Patcher](Create-a-Patcher.md), then you will have a project that has a minimal basic setup:

```cs
public static async Task<int> Main(string[] args)
{
    return await SynthesisPipeline.Instance
        .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
        .SetTypicalOpen(GameRelease.SkyrimSE, "YourPatcher.esp")
        .Run(args);
}

public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
{
    //Your code here!
}
```

## Run Patch Method
The `RunPatch` method is where the code for your patcher should be located.  It will be run when Synthesis is running a pipeline, as well as when your program is started standalone.  The method has access to the Synthesis State object, which has much of the information needed to run a patcher.  

Whatever changes you want to be made should be applied to the `state.PatchMod` object.

## Synthesis State Object
The state object given to your `RunPatch` contains several important objects:

- PatchMod: The export patch mod object that all changes should be applied to
- [LoadOrder](https://mutagen-modding.github.io/Mutagen/loadorder/):  Object containing all the readonly mod objects on the load order
- [LinkCache](https://mutagen-modding.github.io/Mutagen/linkcache/):  Link Cache created from the load order
- ExtraSettingsDataPath:  Path where any custom [Internal Data](Internal-Data.md) will be located

## Typical Simple Code
Typical code for a Synthesis patcher consists of locating Winning Overrides for a record type, and adding them to the output patch mod with some changes.  Here is a simplistic example:

```cs
// Loop over all the winning NPCs in the load order
foreach (var npcGetter in state.LoadOrder.PriorityOrder.Npc().WinningOverrides())
{
    // See if it is a Goblin 
    if (!npcGetter.EditorID?.Contains("Goblin", StringComparison.OrdinalIgnoreCase) ?? true) continue;

    // Add it to the patch
    var npc = state.PatchMod.Npcs.GetOrAddAsOverride(npcGetter);

    // Shrink
    npc.Height /= 2;
}
```

The above code will shrink any Goblin-named NPCs

Two good resources for learning further details:

- A large catalogue of [existing patchers](https://github.com/Mutagen-Modding/Synthesis/network/dependents?package_id=UGFja2FnZS0xMzg1MjY1MjYz) to look at for examples
- In-depth Mutagen documentation found on the [Mutagen wiki](https://mutagen-modding.github.io/Mutagen/).
