<!-- START doctoc generated TOC please keep comment here to allow auto update -->
<!-- DON'T EDIT THIS SECTION, INSTEAD RE-RUN doctoc TO UPDATE -->
## Table Of Contents

- [Local Solution Patcher](#local-solution-patcher)
  - [Goals and Reasons to Choose](#goals-and-reasons-to-choose)
  - [Required Input](#required-input)
  - [New Patcher Wizard](#new-patcher-wizard)
  - [Patcher Settings](#patcher-settings)

<!-- END doctoc generated TOC please keep comment here to allow auto update -->

# Local Solution Patcher
![](https://i.imgur.com/FUGCqsN.png)

## Goals and Reasons to Choose
This style uses a local C# project which is compiled and run as a patcher.

**This is the recommended patcher type for developers**

- Preferred route any time you want to run raw code from a folder that you downloaded/created yourself
- Can create new patcher Solutions/Projects or latch on to existing projects
   - Generated solutions come with a lot of initial settings configured for you, such as Nullability errors.
- Code can be developed in an IDE on the side, but run from within Synthesis as part of a larger patcher pipeline. 
- Meta information tooling is offered to customize the patcher's information such as descriptions/nicknames/visibility in the patcher browser

## Required Input
The basic input required is:
- Path to a solution
- A dropdown of related projects will populate, of which one should be chosen.

## New Patcher Wizard
Synthesis is able to construct new patcher projects for you that contain a lot of starting frameworks and default settings.

![](https://i.imgur.com/06H1CRa.png)

It is able to construct whole Solutions from scratch, or add a new project to an existing solution, or latch onto existing projects.

## Patcher Settings
A patcher has a Synthesis specific meta file where you can specify patcher description, among other things.  The Solution Patcher has built in GUI controls for modifying/creating this file:

![](https://i.imgur.com/mTJevUM.png)

Settings you can modify:
- Patcher display name
- One line description
- Multi-line extended description
- Whether to show in the [patcher browser](https://github.com/Mutagen-Modding/Synthesis/wiki/Git-Repository#patcher-browser) by default
