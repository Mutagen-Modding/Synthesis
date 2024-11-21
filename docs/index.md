# What does Synthesis do?
Synthesis allows modders to develop mods via code, rather than by hand.  These are often referred to as "patchers".

As a user creating a modded load order, Synthesis lets you add as many of these patcher mods as you like, and bundle them together into one single mod file: `Synthesis.esp`.  You should rerun the Synthesis pipeline any time you add/remove mods so the patchers can change and adapt to the new content.

[![](https://discordapp.com/api/guilds/759302581448474626/widget.png)](https://discord.gg/53KMEsW)

# For Users:
Easily add patchers and run patcher pipelines on your game.  

![Showcase](images/showcase.gif)

[:octicons-arrow-right-24: Installation](Installation.md)

[:octicons-arrow-right-24: Typical Usage](Typical-Usage.md)

# For Developers:
Utilize development tools provided by Synthesis to help you create Mutagen patchers from scratch

```cs
// Loop every NPC in the game
foreach (var npc in state.LoadOrder.PriorityOrder.Npc().WinningOverrides())
{
    // Add the record as an override to the new patch
    var overrideNpc = state.PatchMod.Npcs.GetOrAddAsOverride(npc);
	
	// Add 10% to their height
    overrideNpc.Height *= 1.1f;
}
```

[:octicons-arrow-right-24: Create a Patcher](devs/Create-a-Patcher.md)