# Installation
## Install Latest .NET SDK
You can get the typical SDK installation from Microsoft's official page

[:octicons-arrow-right-24: Download SDK](https://dotnet.microsoft.com/download)

!!! bug "Avoid install/uninstalling SDKs repeatedly"
    If after installing the .NET SDK as instructed above it doesn't work, try following [this FAQ first](https://github.com/Mutagen-Modding/Synthesis/discussions/135)

!!! tip "Restart"
    It's usually a good idea to restart your computer after installing DotNet SDK to help things settle in.
	
### Runtime Is Not The SDK

- `.NET Runtime` -> Enables you to you run existing .NET programs (such as the Synthesis UI)
- `.NET SDK` -> Enables you to compile code

Some users get confused and just install the `.NET Runtime`.  The `Runtime` is required, but Microsoft itself will prompt you to install this when trying to start the Synthesis UI, if it is missing.

Be sure to install the `SDK`; The `Runtime` alone is not enough.  

## Download Synthesis UI
To download Synthesis itself, go is the GitHub release section

[:octicons-arrow-right-24: Download Synthesis UI](https://github.com/Mutagen-Modding/Synthesis/releases)

On the latest release, download _just_ the **Synthesis.zip** file.  The other files are not needed.

Unzip the archive somewhere you like

!!! bug "Unzip All Files"
    Make sure you bring along ALL the files within the zip, not just `Synthesis.exe`

!!! tip "Dedicated Folder"
    The app will create a few files nearby itself, so it is good to put it within its own folder.

## Run Synthesis
You're ready to run Synthesis!

![Showcase](images/showcase.gif)

### Are You a User?

Be sure to read the rest of the wiki for how to use the app.

[:octicons-arrow-right-24: Typical Usage](Typical-Usage.md)

!!! info "SDK Problems?"
    Perhaps the SDK installation had some issues.  Check out the [FAQ on the topic](https://github.com/Mutagen-Modding/Synthesis/discussions/135)

### Are You a Developer?
There are a lot of resources on how to get started creating a patcher

[:octicons-arrow-right-24: Create a Patcher](devs/Create-a-Patcher.md)
