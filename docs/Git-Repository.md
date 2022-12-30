<!-- START doctoc generated TOC please keep comment here to allow auto update -->
<!-- DON'T EDIT THIS SECTION, INSTEAD RE-RUN doctoc TO UPDATE -->
## Table Of Contents

- [Git Repository Patcher](#git-repository-patcher)
  - [Goals and Reasons to Choose](#goals-and-reasons-to-choose)
  - [Required Input](#required-input)
  - [Patcher Browser](#patcher-browser)
  - [Versioning](#versioning)
    - [Mutagen/Synthesis Version](#mutagensynthesis-version)
      - [Latest](#latest)
      - [Match](#match)
      - [Manual](#manual)
    - [Patcher Version](#patcher-version)
      - [Latest](#latest-1)
      - [Tag](#tag)
      - [Branch](#branch)
      - [Commit](#commit)

<!-- END doctoc generated TOC please keep comment here to allow auto update -->

# Git Repository Patcher
**This is the recommended patcher type for typical users.**

This type latches on to a patcher accessible via a Git Repository address (usually hosted on something like Github.com).  ****It will clone the code and build the exe to run locally on your machine****.  It only supports Mutagen-based projects.

![](https://i.imgur.com/DdgARsi.png)

## Goals and Reasons to Choose
Because it builds code on your machine, it has a few upsides:
- Can automatically update to the latest version of a patcher's code.
- Can artificially update a patcher to the latest version of Mutagen/Synthesis to grab bugfixes/optimizations.
- Does not require a patcher author to explicitly create and publish an exe to be runnable.

## Required Input
- Address of the git repository
- Project within the repository to use (a repository could have multiple patchers, for example)

## Patcher Browser
Alternatively, you can make use of the "Browse" feature, which lists a whole load of patchers that were automatically located on Github.

![](https://i.imgur.com/S0JsBgV.png)

To get to the Patcher browser, click and add a new Git Repository Patcher at the top left, and then go to the Browse tab.

## Versioning
Git Repository Patchers have the unique capability of being able to control what code you want to use for a patcher.

![](https://i.imgur.com/DpcHKDN.png)

Some of these concepts relate closely to Git concepts.

### Mutagen/Synthesis Version
This controls what versions of the Mutagen (mod parsing) and Synthesis (patching pipeline) libraries to force the patcher to use.

#### Latest
![](https://i.imgur.com/kEAsewh.png)

Force the patcher to use the latest published libraries.

Pros:
- Get the latest bugfixes
- Get the latest speed optimizations
- Synthesis system can string patchers together with the same versions for extra speed.  (still in development)

Cons:
- Patcher might fail to compile if things changed too much

#### Match
![](https://i.imgur.com/6xi1jxs.png)

Use whatever versions were listed explicitly by the patcher.

Pros:
- More stable, as these were the versions used when the patcher was developed

Cons:
- Won't have any fixes or optimizations that came later on
- Won't hook into the systems that speed up patching when all patchers are on the same version (still in development)

#### Manual
![](https://i.imgur.com/fafV7mK.png)

As a user, you have full control over the explicit version you want the patcher to use.  If for some reason you know the exact version you want to use, you can input it here manually.

### Patcher Version
This controls what version of the patcher's code to use.  As a patcher developer modifies their patcher with new features and functionality, this section lets you control when/how/what version of the patcher you want to use.

#### Latest
![](https://i.imgur.com/QnINzUg.png)

Run the latest code the patcher has to offer from its default main branch

Pros:
- Get the latest features/fixes/optimizations right as they come out

Cons:
- The resulting patch might change at any time if a patcher updates and changes its logic.  **If you want a consistent patch every time you run, you don't want to use this option.**

#### Tag
![](https://i.imgur.com/i5JWbGB.png)

Patcher developers can opt to stamp their code with Tags to represent version releases.  For example, they could release `v1.0`, `v1.1`, etc.  This option lets you select a specific version of their patcher to use, and allows you to quickly swap between versions at will.  Note that not all patcher developers will make use of Tags, though.

Pros:
- Stable, as it will use code that relates to a specific release/version

Cons:
- Not all patcher developers may use these systems
- Will not automatically update if new versions come out

#### Branch
![](https://i.imgur.com/GzAeLBC.png)

Patcher developers can create temporary "branches" of code to test new features or contain the latest experimental code that might not be ready to release.  A user can latch on and use the code from any branches the patcher developer has created.  If talking with a patcher developer and they tell you to try out their experimental code, you can easily swap over and use it by specifying the branch name they are working on.

Pros:
- Ability to swap to experimental versions of a patcher that don't have explicit tagged releases yet

Cons:
- Code can change in between patcher runs, if the patcher developer pushes new code to the branch.
- Branch can be deleted by the patcher developer at any time.  You will have to pick a new branch or versioning mode, then.

#### Commit
![](https://i.imgur.com/4fwTOXc.png)

This lets a user target and use code from a very specific point in history, no matter if the patcher developer tagged it or has a branch pointing to it.  If you know the exact code state you want to use, you can use it by specifying the commit Sha related to that point in history.

Pros:
- Explicit control over the patcher code to use

Cons:
- Might be hard to find the desired sha you want to use