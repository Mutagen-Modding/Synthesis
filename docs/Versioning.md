# Versioning

Synthesis has a few distinct versioning concepts.  These are important for understanding what code will be run when making your patches.

## UI Version
Synthesis itself has an exe that is the GUI that you interact with.  The version the UI is running is what is listed at the top right of the window.

![](https://i.imgur.com/iIttCQ7.png)

This picture shows a UI that is `0.21.2`

!!! tip "Use the newest UI"
    The UI should usually always be the newest stable version available.  Only in rare circumstances might you want to downgrade it if some bug was encountered, and a hotfix hadn't been released yet.

## Git Patcher Versioning
This section outlines the various ways a specific Git Patcher's versioning can controlled.   As this is the most commonly used patcher, it is an important topic.

### Libraries to Use
`Mutagen` and `Synthesis` are also code libraries that patchers use to develop their logic.  Depending on which version a patcher uses when running, it might get certain fixes/improvements/features.

- `Mutagen` library that allows mods to read and write `esp` files and other similar tasks
- `Synthesis` library that helps bootstrap a project to easily hook into the UI and be runnable by users.

Each patcher in Synthesis' UI comes with controls to modify what versions of the libraries a patcher will be run with.
![](https://i.imgur.com/UjVo97u.png)

#### Profile
This option is only available on individual patchers, and helps keeps all of your patchers in sync.  The profile settings will have one single versioning choice, and all patchers set to `Profile` will follow along automatically.   This allows easy control from one location.

![](https://i.imgur.com/Fa3zrYr.gif)

!!! success "Recommended"
    This is the recommended choice for individual patchers [Read More](#recommended-setup)

#### Manual
Sets the patcher to use a specific version, while allowing the user to easily update to latest when desired.

Pros:

- Follows latest easily, while giving you a heads up and control over timing

Cons:

- Depending on the versions you input and the age of the patcher, it may not compile

![](https://i.imgur.com/u1xRQwE.gif)

!!! success "Recommended"
    Manual is the best balance between control and convenience, and is the recommended default choice [Read More](#recommended-setup)
 
#### Match
Use whatever versions were listed explicitly by the patcher.

Each patcher is coded at a certain point in time.  The developer will typically work with the newest versions when develop their patcher.  As time progresses this listed version will become "old" as newer versions of `Mutagen` or `Synthesis` get released.

By setting a patcher to `Match`, you are telling a patcher to use the same versions it was originally coded with.  This will be the most compatible, but might miss out on some necessary bugfixes.

Pros:

- More stable, as these were the versions used when the patcher was developed

Cons:

- Won't have any fixes or optimizations that came later on


!!! info "Compatibility Fallback Choice"
    Only set patchers to Match if they are having problems running
   
#### Latest
This will upgrade the patcher to use the latest version of Mutagen/Synthesis libraries automatically.  It's usually recommended to avoid using this, in favor of `Manual`, which lets you click the upgrade buttons yourself so you know when things are being upgraded.

Pros:

- Get the latest features/fixes/optimizations right as they come out

Cons:

- The resulting patch might change at any time if a patcher updates and changes its logic.
- Depending on the age of the patcher, it may not compile

!!! warning "Unexpected Updates"
    If you want a consistent patch every time you run, you don't want to use this option

#### Recommended Setup
The recommended setup for patcher versioning is:

- In profile settings, set to `Manual`
- Press `Reset Patchers to Profile`, if you want to snap them all to follow the profile settings.
- Hit the blue upgrade button to upgrade Mutagen/Synth to newest at your leisure when it makes sense for you
- Any single patchers that can't run can be individually set to `Match` for maximum compatibility

![](https://i.imgur.com/QMA0bNI.png)

#### Using Prerelease Versions
Sometimes you might want to use a new experimental version of the libraries.  To allow this, check the `Prerelease` checkbox in the profile settings

![](https://i.imgur.com/pT8Snpt.gif)

### Patcher Code to Run
The last aspect of versioning is the patcher code itself.  This is the code the developer wrote -using- `Mutagen` and `Synthesis` to accomplish a specific goal. 
There are a few options available for controlling what patcher code you want to be run.

Most of the concepts here are `Git` concepts and lingo, which is the system that helps version code and let you "travel back in time" to past states.

#### Branch
Branches are nicknamed trails of code over time.  Typically the `main` branch has the most stable code, while other branches like `dev` might exist with more experimental work, but it is up to the patcher developer.

By choosing `Branch` and providing a name, you are instructing Synthesis to follow that named path of code.  If a developer pushes new code to a branch, Synthesis will want to update and follow along.

![](https://imgur.com/9NzFYXH)

!!! success "Recommended"
    `Branch` mode, with `Auto -> Off`, and `Main -> On` is the recommended setup

On the left you can see the name of the branch being followed: `master`

The `Auto` checkbox will control whether to follow the branch automatically, or notify you with a blue arrow when one is available.  In this picture, `Auto` is off, and so the UI is showing a blue arrow offering to update.

The `Main` checkbox locks the name of the branch to whatever the patcher decides is its main branch.  This avoids the need to type in the exact branch name by hand.  If you want to switch to another branch, you will need to uncheck this `Main` box, which will allow you to type in the alternative branch name.


#### Tag
A tag is a nicknamed state of the code at a specific point in time.  Typically it will never move, and will always refer to the same code.  Often tags will be named things like `v1.1`, `v1.2`, and represent discrete versions of the code to choose from.

![](https://imgur.com/I6TKx28)

You can type in the name of the tag that you would like to use, and then press the blue arrow to update to that tag.

If the patcher developer has named their tags like versions, then Synthesis can be set to `Auto` and will update to the latest tag whenever one is released.

!!! info "Not Always Available"
    It is up to the developer to stamp tags if they would like to.  They may not decide to do so, and so this option will not be viable.

#### Commit
A commit is a specific point in time.  By specifying a single commit, you are instructing Synthesis to use the code as it existed at a very specific point in time.

![](https://imgur.com/WTBlsd8)

This type of extreme control is typically not needed.   The use cases for using `Commit` are:

- The patcher developer instructs you to do so to test something
- The patcher developer isn't maintaining the other options properly, and you know of a specific state of the code that you want to use.
