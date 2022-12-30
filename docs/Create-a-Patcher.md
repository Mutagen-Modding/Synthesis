<!-- START doctoc generated TOC please keep comment here to allow auto update -->
<!-- DON'T EDIT THIS SECTION, INSTEAD RE-RUN doctoc TO UPDATE -->
## Table Of Contents

- [Install an IDE](#install-an-ide)
- [Solution Patcher](#solution-patcher)
- [Locating the Code](#locating-the-code)
- [Customization](#customization)
- [More Topics](#more-topics)

<!-- END doctoc generated TOC please keep comment here to allow auto update -->

## Install an IDE
**Be sure to install an [IDE](https://visualstudio.microsoft.com/vs/community/) for C# development!** You will also need the [DotNet SDK](https://dotnet.microsoft.com/download), but this should come with the IDE.

## Solution Patcher
Synthesis provides bootstrapping functionality to get new Mutagen patchers off the ground fairly easily.

These systems can be found by creating a new [Local Solution patcher](https://github.com/Mutagen-Modding/Synthesis/wiki/Local-Solution), which is the preferred patcher type for developers.

![](https://i.imgur.com/06H1CRa.png)

There is a few options available when creating a new Solution Patcher:
- New Solution and Project (for brand new setups)
- New Project (for adding another patcher to an existing repository)
- Existing (latch on to an existing setup)

When creating a new patcher, Synthesis will construct and set up several default settings/files:
- Creates a solution
- Creates a project, with both Synthesis and Mutagen imported
- Creates a default main method, which is hooked into the standard Synthesis pipeline.
- Enables [nullability compiler features](https://github.com/Mutagen-Modding/Mutagen/wiki/Classes%2C-Interfaces%2C-and-Record-Presence#nullability-to-indicate-record-presence), which are very important when utilizing Mutagen
- Default gitignore
- Hides/upgrades some specific error types

## Locating the Code
The Local Solution Patcher can open your preferred IDE, or you can navigate and open the solution yourself.

A patcher project should have a `Program.cs` file with the initial Synthesis bootstrapping code already in place.

```csharp
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

The main function calls Synthesis bootstrapping code that listens for commands from the Synthesis pipeline and calls your RunPatch code when appropriate.

## Customization
Patchers that are listed on the store use a meta json file to specify some customization.  The Local Solution Patcher can modify this file for you in the GUI itself.

![](https://i.imgur.com/iA9DR89.png)

- Display Name
- One line description
- Long description
- Whether to show in the store by default

## More Topics
[[Coding a Patcher]]

[Running a Patcher](https://github.com/Mutagen-Modding/Synthesis/wiki/Running-and-Debugging)