<!-- START doctoc generated TOC please keep comment here to allow auto update -->
<!-- DON'T EDIT THIS SECTION, INSTEAD RE-RUN doctoc TO UPDATE -->
## Table Of Contents

- [Adding Patchers](#adding-patchers)
  - [Select a Patcher Type](#select-a-patcher-type)
  - [Locate a Patcher](#locate-a-patcher)
    - [Browse](#browse)
    - [Input Tab](#input-tab)
    - [Using .Synth files](#using-synth-files)
- [Customizing Patcher Input](#customizing-patcher-input)
- [Familiarize Yourself With the Versioning Systems](#familiarize-yourself-with-the-versioning-systems)
- [Running the Patcher Pipeline](#running-the-patcher-pipeline)
- [Enable in your Load Order](#enable-in-your-load-order)
  - [Placement matters](#placement-matters)

<!-- END doctoc generated TOC please keep comment here to allow auto update -->

This clip is a good visual example of typical usage, but this page will be going over it in more detail.

![Typical Usage](https://i.imgur.com/Wj2fGaF.gif)

# Adding Patchers
## Select a Patcher Type
There are a few alternatives, but the one recommended for new users is the Git Repository patcher.

![Git Repository Patcher](https://i.imgur.com/LP2Q9jy.png)

You can read about the other types of patchers [here](https://github.com/Mutagen-Modding/Synthesis/wiki/Patcher-Types).

## Locate a Patcher
### Browse
You can either look around in the wild yourself, or you can make use of the built in list of existing patchers.  Within a `Git Repository` patcher, there is a `Browse` section with an automatically populated list:

![Patcher Browser](https://i.imgur.com/63IgcRf.png)

### Input Tab
Alternatively, if you have the github address you want to use you can paste it directly into the `Input` tab.

### Using .Synth files
Alternatively, Synthesis patchers can be distributed via `.synth` files.  These files simply instruct the program to add the patcher as if you did the above steps.

To use a `.synth` file:
- Open Synthesis
- Select the group you want to add the patcher to.  Initially there is only one, so you can skip this step
- Double click the `.synth` file on the desktop

![](https://i.imgur.com/1bQ23Zu.gif)

# Customizing Patcher Input
Some patchers either require extra input, or offer customization options.  Some use the settings panel within Synthesis itself, while others use extra json files.

The settings files for a given patcher can be found in:

**[Path To The Synthesis Exe]/Data/[Profile Name]/[Patcher Name]/**

Each patcher will expose their settings files in different ways, so refer to the specific patcher for documentation.   Note that not all patchers have extra settings.

# Familiarize Yourself With the Versioning Systems
Synthesis can artificially upgrade patchers to newer versions of Mutagen, and each patcher can potentially have its own specific settings for what Mutagen version it will use.  Typically it's best to keep it simple and follow the [typical recommended setup](https://github.com/Mutagen-Modding/Synthesis/wiki/Versioning#recommended-setup).  This will give you precise control over when you upgrade, while allowing you to revert patchers that have problems running into hyper compatibility mode.

# Running the Patcher Pipeline
Once you have a list of patchers, you can run them to create a single `Synthesis.esp` patch file which will contain all the changes from the patchers.  

![Running the Pipeline](https://i.imgur.com/EiKcWex.gif)

You can click on any specific patcher to see specifics about it, or any errors it may have printed. 

# Enable in your Load Order
Once a patch is created, you'll want to use your favorite tool and make sure the results look good and then add the patch to your load order!
## Placement matters
Be aware that the location you put the patch in your load order matters.  [Read more here](https://github.com/Mutagen-Modding/Synthesis/wiki/Load-Order-and-Previous-Patchers)