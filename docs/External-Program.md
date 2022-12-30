<!-- START doctoc generated TOC please keep comment here to allow auto update -->
<!-- DON'T EDIT THIS SECTION, INSTEAD RE-RUN doctoc TO UPDATE -->
## Table Of Contents

- [External Program Patcher](#external-program-patcher)
  - [Goals and Reasons to Choose](#goals-and-reasons-to-choose)
  - [Required Input](#required-input)

<!-- END doctoc generated TOC please keep comment here to allow auto update -->

# External Program Patcher
This type latches on and executes any executable program (exe).  It will pass along the Synthesis [command line arguments](https://github.com/Mutagen-Modding/Synthesis/wiki/CLI-Specification) to inform the program of what it should be patching, and where to put its results.  

![](https://i.imgur.com/HpOAsQ4.png)

## Goals and Reasons to Choose
This patcher type is meant for non-Mutagen based patcher programs that want to participate in a Synthesis patch pipeline.  As long as the program can take in the [command line arguments](https://github.com/Mutagen-Modding/Synthesis/wiki/CLI-Specification), and produce a patch file in the desired location, it can be a patcher within Synthesis.

This patcher type is not typically used with Mutagen-based patchers, as [[Git Repository]] or [Local Solution](https://github.com/Mutagen-Modding/Synthesis/wiki/Local-Solution-%28Dev%29) are better options.

## Required Input
The only required input is a path to the executable file to run.