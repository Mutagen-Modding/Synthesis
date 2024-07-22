# Updating the Synthesis UI
There are a few [Versioning](Versioning.md) concepts within the Synthesis ecosystem, but the UI itself should generally be kept up to date with the newest version.

## Update .Net SDK
If the [Installation Instructions](Installation.md) have updated, you might need to install the newest .Net SDK

## Updating the UI
- Go to [Releases](https://github.com/Mutagen-Modding/Synthesis/releases) and download the latest stable version.
- Optionally back up your settings.  This includes:
    - `[UI Exe Location]/PipelineSettings.json`
    - `[UI Exe Location]/GuiSettings.json`
    - `[UI Exe Location]/Data/`
- Unzip the release zip
- Overwrite the old Synthesis files
- Can also opt to keep the new version in a new area, but you will want to copy over the settings mentioned above
