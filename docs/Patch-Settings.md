# Patch Settings

These settings control how Synthesis exports your final patch files.

## Lower FormID Range

This setting controls whether Synthesis should allow using the "lower" FormID ranges (0x000000 - 0x000800).

**Default:** Auto (recommended)

### What are Lower FormID Ranges?

FormIDs in the lower range (0-0x800, typically) are usually reserved by the game engine for hardcoded records. Using these FormIDs can potentially cause issues with game stability.

### Available Options

- **Auto** - Let Synthesis automatically detect whether lower FormID ranges should be used based on your load order and game requirements
- **Disallow** - Never use lower FormID ranges (safest option)
- **Allow** - Permit usage of lower FormID ranges if needed

### When to Change

In most cases, the **Auto** setting is recommended. Only change this if you have specific knowledge about your load order requirements or are troubleshooting FormID-related issues.

## Master Version Override

This setting allows you to export patches with a specific header version instead of using the latest version for your game.

**Default:** Empty (uses latest version)

### Usage

- **Empty/Blank** - Uses the latest header version for your game (recommended)
- **Specific Version** - Enter a version number (e.g., `1.70`) to force that header version

### Important Notes

- **Not recommended to change** - Modern tools and games expect current header versions
- Only modify this setting if you have a specific compatibility requirement with older tools
- Incorrect header versions may cause crashes or loading issues
- This is an advanced setting for edge cases only

## Master Flag

This setting controls whether your patch file is exported with the Master flag enabled.

**Default:** OFF (disabled)

### What is the Master Flag?

The Master flag marks a plugin file as a "master" file in Bethesda games. Master files:

- Load before regular plugin files
- Can be referenced by other plugins as dependencies
- Are typically used for base game files and major overhauls

### When to Enable

- **Generally leave OFF** - Most Synthesis patches should be regular plugins, not masters
- Only enable if you specifically need other plugins to reference your Synthesis patch as a master dependency
- Enabling this unnecessarily can complicate your load order

## Location in Settings

These settings are found in the Profile settings under the **Patch Settings** section, located above the Compaction settings.
