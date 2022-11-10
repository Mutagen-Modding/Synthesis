<!-- START doctoc generated TOC please keep comment here to allow auto update -->
<!-- DON'T EDIT THIS SECTION, INSTEAD RE-RUN doctoc TO UPDATE -->
## Table Of Contents

- [Install .NET 6.0 SDK](#install-net-60-sdk)
    - [Do NOT install/uninstall multiple SDKs repeatedly](#do-not-installuninstall-multiple-sdks-repeatedly)
- [Restart](#restart)
- [Download Synthesis](#download-synthesis)
- [Run Synthesis](#run-synthesis)
  - [Having SDK Problems?](#having-sdk-problems)
  - [Are You a User?](#are-you-a-user)
  - [Are You a Developer?](#are-you-a-developer)

<!-- END doctoc generated TOC please keep comment here to allow auto update -->

# Install .NET 6.0 SDK
A good place to go for this is:
https://dotnet.microsoft.com/download

**Be sure to choose .NET 6.0 SDK:**

![Highlight of the download you should choose](https://i.imgur.com/Zc8a7ZZ.png)

Installing just the .NET 6.0 Runtime will let you open Synthesis, but it will not be able to build patchers and run them.  **The SDK is required for the whole pipeline to work as intended.**

### Do NOT install/uninstall multiple SDKs repeatedly
If after installing the .NET 6.0 SDK as instructed above it doesn't work, try following [this FAQ first](https://github.com/Mutagen-Modding/Synthesis/discussions/135)

# Restart
It's usually a good idea to restart your computer after installing DotNet SDK.  Helps things settle in.

# Download Synthesis
Currently the place to go is [the release page](https://github.com/Mutagen-Modding/Synthesis/releases)

On the latest release, download _just_ the **Synthesis.zip** file.  The other files are not needed.

Unzip the archive somewhere you like.  Make sure you bring along ALL the files within the zip, not just `Synthesis.exe`

It will create a few files nearby itself, so it is good to put it within its own folder.

# Run Synthesis
You're ready to run Synthesis!

![Typical Usage](https://i.imgur.com/Wj2fGaF.gif)

## Having SDK Problems?
Check out the [FAQ on the topic](https://github.com/Mutagen-Modding/Synthesis/discussions/135)

## Are You a User?
Check out the [[Typical Usage]]

## Are You a Developer?
[[Create a Patcher]]