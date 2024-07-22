# Nexus Integration
The Nexus and other sites typically want a file to be uploaded in order to make a mod.   Synthesis patchers typically do not have a concrete set of files to upload, and we would prefer the source code be located on sites like GitHub, which have much more robust code versioning systems.

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