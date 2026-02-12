# Master Overflow Settings

## Split Files if Max Masters Exceeded
This setting allows Synthesis to automatically handle the `TooManyMastersException` by detecting and merging split mod files that were created when a patcher exceeded the master file limit (typically 255 masters in Skyrim).

**Default:** ON (enabled by default)

## When to Enable

- Turn this ON (default) if you encounter `TooManyMastersException` errors during pipeline execution
- This allows patchers to create multiple split files (e.g., `Patch_1.esp`, `Patch_2.esp`) when they exceed the master limit
- The next patcher in the group will automatically detect and merge these split files back together

## Important Requirements

- **All patchers in the group must target a Synthesis version that supports this feature** (v0.36.0 or later)
- If you have older patchers that need to target older Synthesis versions:
  - Place them earlier in the patcher group (before the master limit is reached)
  - This ensures they run successfully before split files are created
  - Later patchers with modern Synthesis versions can handle the split files

## How It Works

1. When a patcher exceeds the master limit, it creates split files: `ModName_1.esp`, `ModName_2.esp`, etc.
2. Each split file contains a subset of the masters to stay under the limit
3. The next patcher with this setting enabled will:
   - Detect the split files
   - Merge their contents back together
   - Present them as a single unified mod in the load order
   - Continue patching normally

## Example Scenario
```
Group Order:
1. OldPatcher (v0.35.0) - runs first, doesn't create split files
2. HeavyPatcher (v0.36.0) - adds many masters, creates Patch_1.esp and Patch_2.esp
3. FinalPatcher (v0.36.0) - merges split files and continues
```

In this example:

- `OldPatcher` runs with v0.35.0 (doesn't support split files) and completes successfully
- `HeavyPatcher` hits the master limit and creates `Patch_1.esp` and `Patch_2.esp`
- `FinalPatcher` automatically detects and merges the split files, then continues patching normally

## Troubleshooting

### Error: "Could not find file 'ModName.esp'"
This error typically means:

- A patcher created split files (e.g., `ModName_1.esp`, `ModName_2.esp`) but the next patcher doesn't support the split file feature
- **Solution**: Ensure all patchers in the group use Synthesis v0.36.0 or later, OR place older patchers earlier in the group

### Mixed Version Scenarios
If you must use older patchers (< v0.36.0) alongside newer ones:

1. **Analyze your load order**: Identify which patchers are likely to cause the master limit to be exceeded
2. **Place old patchers first**: Put all v0.35.x and older patchers at the beginning of your group
3. **Place heavy patchers last**: Patchers that add many masters should come after old patchers
4. This ensures old patchers run before split files are created

---

## Update Load Order After Run
When split files are created, your `Plugins.txt` needs to be updated to include the new split files so that the game and other tools recognize them. This setting automates that process.

**Default:** ON (enabled by default)

After a successful run, Synthesis will update `Plugins.txt` to reflect the current set of output files:

- **Adds** any new split files (e.g., `Patch_2.esp`, `Patch_3.esp`) that were created
- **Removes** old split file entries that are no longer produced
- **Preserves** the original position and enabled/disabled state of the base mod in the load order
- New entries are inserted adjacent to the base mod entry so that split files remain consecutive
