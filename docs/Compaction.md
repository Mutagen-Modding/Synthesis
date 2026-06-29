# Compaction

Compaction settings control how your patch files store records, affecting how many mods can be loaded and how many records each mod can contain.

## Overview

Modern Bethesda games use compaction flags to determine how records are stored in plugin files. The game engine can load more mods with smaller compaction levels, but each mod can define fewer total records. This creates a trade-off: smaller compaction allows more mods in your load order, but each individual mod can contain fewer records.

**Default:** Full - This allows the maximum number of records per patch while keeping configuration simple.

## Compaction Style

Select the compaction level for your patches:

- **Small/Light** - Best for small patches that modify few records
- **Medium** - Balanced option for moderate-sized patches (Starfield only)
- **Full** - Best for large comprehensive patches (default)

Choose the smallest compaction level that can accommodate your patch size to maximize the number of mods your load order can support.

**Game Availability:**

- **Skyrim/Fallout 4/Fallout 76:** Small and Full only
- **Starfield:** Small, Medium, and Full

## Fallback on Overflow

When enabled, Synthesis will automatically switch to a larger compaction level if your patch exceeds the record limit for the selected level.

**Example:** If you select "Small" but your patch exceeds the Small record limit, Synthesis will automatically export as "Medium" or "Full" instead.

**Default:** Enabled (recommended)

!!! warning "Starfield Compatibility"
    Starfield is less flexible with compaction styles. Changing compaction levels can cause compatibility issues with existing saves.

    - **Fallback on Overflow is NOT recommended** for Starfield
    - Avoid changing compaction settings frequently
    - Choose your target compaction level carefully before starting a playthrough

## FormID Persistence

This setting controls how Synthesis maintains consistent FormIDs across rebuilds of your patch.

- **None** - No FormID persistence (FormIDs may change between rebuilds)
- **Text** - Uses text-based persistence files
- **Binary** - Uses binary persistence files (more efficient)

Consistent FormIDs are important for save game compatibility. If you rebuild your patch and FormIDs change, existing saves may experience issues.

**Default:** Text mode (recommended)

## Location in Settings

These settings are found in the Profile settings under the **Compaction** section.