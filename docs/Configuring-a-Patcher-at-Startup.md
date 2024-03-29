<!-- START doctoc generated TOC please keep comment here to allow auto update -->
<!-- DON'T EDIT THIS SECTION, INSTEAD RE-RUN doctoc TO UPDATE -->
## Table Of Contents

- [AddPatch](#addpatch)
  - [PatcherPreferences](#patcherpreferences)
- [SetTypicalOpen (will be renamed to SetStandaloneOpen)](#settypicalopen-will-be-renamed-to-setstandaloneopen)
- [Run](#run)
- [SetAutogeneratedSettings](#setautogeneratedsettings)
- [AddRunnabilityCheck](#addrunnabilitycheck)
- [SetOpenForSettings](#setopenforsettings)

<!-- END doctoc generated TOC please keep comment here to allow auto update -->

The main method of a patcher is dedicated to calling SynthesisPipeline, which is a builder that lets you customize how and what your patcher is going to run. 

```cs
// A typical main method
public static async Task<int> Main(string[] args)
{
    return await SynthesisPipeline.Instance
        .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
        .SetTypicalOpen(GameRelease.SkyrimSE, "YourPatcher.esp")
        .Run(args);
}
```

The main method as created by the template system can be left as-is for a basic setup, but it can be customized to your needs from there.

## AddPatch
Adds a Game Category to be handled by your patcher, and lets you decide what function should be called when your patcher is being run for that game.  One `AddPatch` is generally required, as it's the entry point into your patcher for the game you want to target.

### PatcherPreferences
This is an optional argument to an `AddPatch` call, which will customize some settings related to that Patch operation.
- IncludeDisabledMods:  Whether to include mods that are disabled on the LoadOrder object _(default false)_
- AddImplicitMasters:  Whether to enable masters that are listed as disabled, but required by an active mod
- NoPatch:  If toggled on, your patcher will skip the export step.  Useful if your patcher is just modifying other unrelated files only.
- Cancel:  (Advanced) If your app wants to be able to cancel an in-progress patching run, for some reason, a cancellation token can be given here.
- Inclusion/ExclusionMods:  (Untested) Allows whitelisting or blacklisting mods from the load order

## SetTypicalOpen (will be renamed to SetStandaloneOpen)
Determines what your patcher will do if it's run standalone, either via running the exe directly from the desktop, or more likely when running it from your IDE for testing.

It takes in which Game to run (if you're targeting multiple), and what mod name to export to.

## Run
Capstone call to the SynthesisPipeline builder which takes in the arguments given to your app when it started and runs the patcher given the rules you've specified.  Nothing will happen if this is missing.

## SetAutogeneratedSettings
Associates a settings file with a settings object.   This allows you to get [easy autogenerated settings](https://github.com/Mutagen-Modding/Synthesis/wiki/User-Input#automatic-settings-ui-system) provided by Synthesis.

The main method just passes the arguments given your program to the Synthesis systems, which handles all the various commands that could be passed in.  Generally, the main method can be left as-is.

## AddRunnabilityCheck
Provides a callback to check if your patcher sees itself as having all the necessary things it needs to run, and block execution until those requirements are satisfied.
[Read More](https://github.com/Mutagen-Modding/Synthesis/wiki/Required-Mods-and-Runnability-Checks)

## SetOpenForSettings
This is a more advanced option, which allows your patcher to be opened as a settings editor.  It requires you to modify your patcher project a lot, so that it is simultaneously a patcher executable as well as a UI application.   If that is set up, then this call provides the entry point to know when to open your application as a UI for settings input.