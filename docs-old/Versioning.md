<!-- START doctoc generated TOC please keep comment here to allow auto update -->
<!-- DON'T EDIT THIS SECTION, INSTEAD RE-RUN doctoc TO UPDATE -->
## Table Of Contents

- [UI Version](#ui-version)
  - [What UI Version to Choose](#what-ui-version-to-choose)
- [Patcher Library Version Controls](#patcher-library-version-controls)
  - [Mutagen and Synthesis Libraries](#mutagen-and-synthesis-libraries)
  - [Controlling a Patcher's Library Versions](#controlling-a-patchers-library-versions)
    - [Match](#match)
    - [Manual](#manual)
    - [Latest](#latest)
    - [Profile](#profile)
  - [Recommended Setup](#recommended-setup)
  - [Using Prerelease Versions](#using-prerelease-versions)
- [Patcher Versioning Controls](#patcher-versioning-controls)
  - [Branch](#branch)

<!-- END doctoc generated TOC please keep comment here to allow auto update -->

Synthesis has a few distinct versioning concepts.  These are important for determining which code will be run and how when making your patches.  This wiki outlines the different concepts.

# UI Version
Synthesis itself has an exe that is the GUI that you interact with.  The version the UI is running is what is listed at the top right of the window.

![](https://i.imgur.com/iIttCQ7.png)

This picture shows a UI that is `0.21.2`

## What UI Version to Choose
The UI should usually always be the newest stable version available.  Only in rare circumstances might you want to downgrade it if some bug was encountered, and a hotfix hadn't been released yet.

# Patcher Library Version Controls
## Mutagen and Synthesis Libraries
In addition to the UI you're familiar with, `Mutagen` and `Synthesis` are also code libraries that patchers use to develop their logic
- `Mutagen` library that allows mods to read and write `esp` files and other similar tasks
- `Synthesis` library that helps bootstrap a project to easily hook into the UI and be runnable by users.

Depending on which version of `Mutagen` and `Synthesis` a patcher uses in its code, it might contain certain fixes/improvements/features.

## Controlling a Patcher's Library Versions
As a user, each patcher in Synthesis' UI comes with controls to modify what versions of the libraries a patcher will be run with.  For example, you could set it to run with the same versions the patcher was coded with.  Or you could choose to "upgrade" the patcher to use the newest versions in order to get the latest fixes and speed improvements.

![](https://i.imgur.com/UjVo97u.png)

### Match
Each patcher is coded at a certain point in time.  The developer lists whatever version they want to work with when they develop their patcher.  These listed versions are what Synthesis refers to as `Match` versions.  As time progresses this listed version will become "old" as newer versions of `Mutagen` or `Synthesis` get released.

By setting a patcher to `Match`, you are telling it to use the same versions of the libraries as it was coded with.  This will be the most compatible, but might miss out on some necessary bugfixes.

### Manual
This setting allows you to control the versioning to be used explicitly by typing in the desired version by hand.  It will also show a blue arrow when there's a newer version than the one you typed, which you can click manually to upgrade, if you so choose.

![](https://i.imgur.com/u1xRQwE.gif)

### Latest
This will upgrade the patcher to use the latest version of Mutagen/Synthesis libraries automatically.  It's usually recommended to avoid using this, in favor of `Manual`, which lets you click the upgrade buttons yourself so you know when things are being upgraded.

### Profile
This option helps keeps all of your patchers in sync.  The profile settings area has the versioning options described above, and all patchers set to `Profile` will follow along.  This keeps you from needing to manage/upgrade each patcher individually. 

![](https://i.imgur.com/Fa3zrYr.gif)

This is the preferred route for most patchers.  Set them all to `Profile`, and set your `Profile` settings to Latest/Manual as per your preference.

## Recommended Setup
The recommended default setup is:
- Go to profile settings, and set to `Manual`
- Press `Reset Patchers to Profile`, if you want to snap them all to follow the profile settings.
- Hit the blue upgrade button to upgrade Mutagen/Synth to newest at your leisure when it makes sense for you
- Any single patchers that can't run can be individually set to `Match` for maximum compatibility

![](https://i.imgur.com/QMA0bNI.png)

## Using Prerelease Versions
Sometimes you might want to use a new experimental version of the libraries.  To allow this, check the `Prerelease` checkbox in the profile settings

![](https://i.imgur.com/pT8Snpt.gif)

# Patcher Versioning Controls
The last aspect of versioning is the patcher code itself.  This is the code the developer wrote -using- `Mutagen` and `Synthesis` to accomplish a specific goal.  There are a few options available for controlling what patcher code you want to be run.

Most of the concepts here are `Git` concepts and lingo, which is the system that helps version code and let you "travel back in time" to past states.

## Branch
