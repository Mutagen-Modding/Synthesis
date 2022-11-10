<!-- START doctoc generated TOC please keep comment here to allow auto update -->
<!-- DON'T EDIT THIS SECTION, INSTEAD RE-RUN doctoc TO UPDATE -->
## Table Of Contents

- [Directly](#directly)
- [Synthesis GUI](#synthesis-gui)
- [Side Testing Environment](#side-testing-environment)
- [Run through MO2](#run-through-mo2)

<!-- END doctoc generated TOC please keep comment here to allow auto update -->

While developing a patcher, you will want to be able to run your code, debug, and see results.

# Directly
Assuming your patcher has defined `ActionsForEmptyArgs` [User Preferences](https://github.com/Mutagen-Modding/Synthesis/wiki/Coding-a-Patcher#user-preferences), the patcher will run if no arguments are passed in.  This means you can run your patcher straight from your IDE or command line.

Notes:
- The output filename will be whatever you set in your `ActionsForEmptyArgs` object, **not** under `Synthesis.esp`
- If no `ActionsForEmptyArgs` is defined, then this mode will not be able to run.

# Synthesis GUI
Patcher solutions can be run from inside the Synthesis GUI, by adding them as a Synthesis patcher.  The recommended type is [Solution](https://github.com/Mutagen-Modding/Synthesis/wiki/Local-Solution) patchers, but [External Program](https://github.com/Mutagen-Modding/Synthesis/wiki/External-Program) can be used, too.

Notes:
- Lets you run your in-development patcher alongside several other patchers to see their combined results
- Not really the best route for testing just your patcher alone; Use the Direct method mentioned above instead.

# Side Testing Environment
Both of the above routes will by default run against your default Plugins.txt and Data folder, which may or may not contain content you want to test against.   One route for testing against a specific load order is to set up a secondary side `Data` folder and `Plugins.txt` load order file.

To tell your patcher to latch onto that side environment, you can utilize the [CLI Arguments](https://github.com/Mutagen-Modding/Synthesis/wiki/CLI-Specification) to give it the details to use.  Here is an example set of arguments:

```
run-patcher --GameRelease SkyrimSE --DataFolderPath "Path/To/Side/Data"
--LoadOrderFilePath "Path/To/Side/Plugins.txt" --OutputPath "Some/Path/YourMod.esp"
```

Notes:
- Useful for testing a specific load order set up for development

# Run through MO2
A lot of the above options can also be executed in the context of Mod Organizer 2's Virtual File System.  Adding Visual Studio as a program to be run, for example, lets you run your patcher against a MO2 managed load order while retaining the ability to debug and step through your code in the IDE.

Notes:
- Currently, starting Visual Studio or Synthesis GUI through MO2 is only good for one run or so before the VFS mapping seems to break down and the apps need to be reopened.  Some research needs to be done to figure out why this isn't more stable over multiple runs.  As such, it is recommended to use a Side Testing Environment during initial development, and then shift to full load orders for final debugging and testing.