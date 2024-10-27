# Supporting Multiple Games

## Supporting Multiple Games
By adding a [2nd (or more) AddPatch commands](https://github.com/Mutagen-Modding/Synthesis/wiki/Configuring-a-Patcher-at-Startup#addpatch) to your SynthesisPipeline you can have your patcher support different game categories.  Note that the generics will be for a different Mod object, and so a 2nd RunPatch callback with the appropriate mod will be required as an entry point
```cs
public static async Task<int> Main(string[] args)
{
    return await SynthesisPipeline.Instance
        .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
        .AddPatch<IFallout4Mod, IFallout4ModGetter>(RunPatch)
        .SetTypicalOpen(GameRelease.SkyrimSE, "YourPatcher.esp")
        .Run(args);
}

public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
{
    //Your Skyrim code here!
}

public static void RunPatch(IPatcherState<IFallout4Mod, IFallout4ModGetter> state)
{
    //Your Fallout4 code here!
}
```

At this point, your patcher supports multiple game types, and can respond accordingly when it is run with either game.

## Code Reuse
The entry points as shown above make it easy to "support" multiple games, but it is still a challenge to properly develop your code in a way that it can be reused for both.  It requires more advanced knowledge of [generics](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/types/generics), [Aspect Interfaces](https://github.com/Mutagen-Modding/Mutagen/wiki/Interfaces-%28Aspect-Link-Getters%29#aspect-interfaces), [Reflection](https://www.tutorialspoint.com/csharp/csharp_reflection.htm), and other similar concepts.

Making multi-game support easier to code without dipping into as many of those advanced concepts is a frontier topic of Mutagen, so feel free to stop by the discord and chat and brainstorm!
