# Nexus Integration
Typically mods are uploaded straight to the Nexus.  However, Synthesis mods are code, which the Nexus isn't as suited for.  Places like GitHub are better homes for Synthesis patchers, as they come with all the tooling that coding developers rely on:

- Git based historical tracking
- Branches for experimental versions
- Easily browsable source code
- Ability for others to contribute fixes/upgrades as Pull Requests

Additionally, the Synthesis system leverages these [Git Patchers](https://github.com/Mutagen-Modding/Synthesis/wiki/Git-Repository) to fulfill many other features:

- Automatic listing in the Synthesis search systems
- Automatic upgrades over time via the [Versioning System](https://github.com/Mutagen-Modding/Synthesis/wiki/Versioning), which reduces the need for maintenance on your end.

## Create a .Synth File
Synthesis offers a `.synth` file that acts as a convenience installer.   The concept is unnecessary if users are familiar with the [Git Repository Browser](Typical-Usage.md#browse) systems.  However, for Nexus, it allows us to create a file that we can upload for our mod listing.

Please refer to the [.synth File](Synth-File.md) page for more details

## Uploading Exes
One alternative is to compile the exe yourself, and upload that to the Nexus.  This is undesirable for a few reasons:

- It cannot get upgraded with Mutagen/Synthesis fixes without manual work on your end
- People cannot confirm the source code that made the exe.  Git Patcher systems compile the code on the users machine, so the code they see in Github is ensured to be the code that they're running.
- Linux users cannot use exes

!!! warning "Not Recommended"
    Uploading exes directly leads to a fragile ecosystem, and is not recommended