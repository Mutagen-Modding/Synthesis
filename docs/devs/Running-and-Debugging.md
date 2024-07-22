# Running and Debugging

## Running Your Code
### Directly
While developing a patcher, you will want to be able to run your code, debug, and see results.  Running directly from the IDE will mean your patcher is running as a normal C# program, with all the developer features you expect.

#### Standalone Run Configuration
Normally the Synthesis UI specifies to your patcher where the output should end up.  When running standalone, your patcher needs this extra information that is usually provided.  

The `SetTypicalOpen` call that comes with the typical template informs your patcher what the desired behavior is when run standalone.

[:octicons-arrow-right-24: Standalone Run Configuration](Configuring-a-Patcher-at-Startup.md/#standalone-open-configuration)

#### Explicit Side Environment
A typical run will look to your default Plugins.txt and Data folder, which may or may not contain content you want to test against.   One route for testing against a specific load order is to set up a secondary side `Data` folder and `Plugins.txt` load order file.

To tell your patcher to latch onto that side environment, you can utilize the [CLI Arguments](CLI-Specification.md) to give it the details to use.  Here is an example set of arguments:

```
run-patcher --GameRelease SkyrimSE --DataFolderPath "Path/To/Side/Data"
--LoadOrderFilePath "Path/To/Side/Plugins.txt" --OutputPath "Some/Path/YourMod.esp"
```


### Solution Patchers via Synthesis GUI
Patcher solutions can be run from inside the Synthesis GUI, by adding them as a Synthesis patcher.  The recommended type is [Solution](https://github.com/Mutagen-Modding/Synthesis/wiki/Local-Solution) patchers, but [External Program](https://github.com/Mutagen-Modding/Synthesis/wiki/External-Program) can be used, too.

Notes:
- Lets you run your in-development patcher alongside several other patchers to see their combined results
- Not really the best route for testing just your patcher alone; Use the Direct method mentioned above instead.

### Run through MO2
Any of the above options can also be executed in the context of Mod Organizer 2's Virtual File System.  Adding Visual Studio as a program to be run, for example, lets you run your patcher against a MO2 managed load order while retaining the ability to debug and step through your code in the IDE.

!!! info "May Require Reopening"
    Visual Studio or Synthesis GUI through MO2 is only good for one run or so before the VFS mapping seems to break down and the apps need to be reopened.  Some research needs to be done to figure out why this isn't more stable over multiple runs

## Debugging Your Code
Debugging is an important part of coding, as it allows you to more quickly get to the bottom of the bugs and behavior of your patcher.

### Console Logging
You can write console lines, which will print to the console during development, and also to the Synthesis UI for end users.

```cs
Console.WriteLine("Hello World");
```

!!! tip "Use for Useful Messages"
    Console logs should also be informative to read for users.  Only add a console line if it helps get an overview of the program's progress without spamming excessively.

### Debugger and Breakpoints
Often a bug will be harder to diagnose or understand.  Rather than logging every last small step your program does to the console, you can utilize your IDE's debugger to investigate your program