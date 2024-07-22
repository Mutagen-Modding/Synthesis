# External Program Patcher
This type latches on and executes any executable program (exe).  It will pass along the Synthesis [command line arguments](CLI-Specification.md) to inform the program of what it should be patching, and where to put its results.  

![](https://i.imgur.com/HpOAsQ4.png)

## Goals and Reasons to Choose
This patcher type is meant for non-Mutagen based patcher programs that want to participate in a Synthesis patch pipeline.  As long as the program can take in the [command line arguments](CLI-Specification.md), and produce a patch file in the desired location, it can be a patcher within Synthesis.


!!! tip "Not for Mutagen-Based Patchers"
    This is not typically used with Mutagen-based patchers, as [Git Repository](Git-Repository.md) or [Local Solution](Local-Solution.md) are better options.

## Required Input
The only required input is a path to the executable file to run.
