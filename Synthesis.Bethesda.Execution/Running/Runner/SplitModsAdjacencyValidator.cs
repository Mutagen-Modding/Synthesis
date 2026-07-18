using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Analysis;
using Mutagen.Bethesda.Plugins.Order;
using Synthesis.Bethesda.Execution.Exceptions;

namespace Synthesis.Bethesda.Execution.Running.Runner;

public interface ISplitModsAdjacencyValidator
{
    void Validate(IList<ILoadOrderListingGetter> loadOrder);
}

/// <summary>
/// Result of split mod adjacency validation
/// </summary>
public record SplitModsValidationResult(
    bool HasError,
    ModKey? BaseModKey,
    IReadOnlyList<ModKey>? AllModKeys);

public class SplitModsAdjacencyValidator : ISplitModsAdjacencyValidator
{
    public void Validate(IList<ILoadOrderListingGetter> loadOrder)
    {
        var result = ValidateLoadOrder(loadOrder.Select(x => x.ModKey).ToList());
        if (result.HasError)
        {
            throw new NonAdjacentSplitModsException(result.BaseModKey!.Value, result.AllModKeys!);
        }
    }

    /// <summary>
    /// Validates that split mods are adjacent in the load order.
    /// Returns validation result without throwing.
    /// </summary>
    public static SplitModsValidationResult ValidateLoadOrder(IReadOnlyList<ModKey> loadOrder)
    {
        // Build a dictionary of base name -> list of (index, ModKey, splitIndex)
        // splitIndex is null for base mod, or the N for _N suffixed mods
        var modsByBaseName = new Dictionary<string, List<(int Index, ModKey ModKey, int? SplitIndex)>>(
            StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < loadOrder.Count; i++)
        {
            var modKey = loadOrder[i];
            var nameWithoutExtension = modKey.Name;

            // Try to find if this is a split file by checking all potential base names
            // A split file has pattern "BaseName_N" where N >= 2
            var lastUnderscore = nameWithoutExtension.LastIndexOf('_');
            if (lastUnderscore > 0)
            {
                var potentialBaseName = nameWithoutExtension.Substring(0, lastUnderscore);

                // Use Mutagen's official split file detection
                if (MultiModFileAnalysis.IsSplitFileName(nameWithoutExtension, potentialBaseName, out var splitIndex)
                    && splitIndex >= 2)
                {
                    if (!modsByBaseName.TryGetValue(potentialBaseName, out var list))
                    {
                        list = new List<(int, ModKey, int?)>();
                        modsByBaseName[potentialBaseName] = list;
                    }
                    list.Add((i, modKey, splitIndex));
                    continue;
                }
            }

            // This could be a base mod (no _N suffix or _1 or _0)
            if (!modsByBaseName.TryGetValue(nameWithoutExtension, out var baseList))
            {
                baseList = new List<(int, ModKey, int?)>();
                modsByBaseName[nameWithoutExtension] = baseList;
            }
            baseList.Add((i, modKey, null));
        }

        // Now check each group for adjacency
        foreach (var kvp in modsByBaseName)
        {
            var entries = kvp.Value;

            // A split set requires:
            // 1. A base mod (splitIndex == null)
            // 2. At least one _N suffixed mod (splitIndex != null)
            var hasBase = entries.Any(e => e.SplitIndex == null);
            var hasSplits = entries.Any(e => e.SplitIndex != null);

            if (!hasBase || !hasSplits)
            {
                // Not a complete split set, skip
                continue;
            }

            // Sort by index to check adjacency
            var sortedByIndex = entries.OrderBy(e => e.Index).ToList();

            // Check that all indices are consecutive
            for (int i = 1; i < sortedByIndex.Count; i++)
            {
                if (sortedByIndex[i].Index != sortedByIndex[i - 1].Index + 1)
                {
                    // Non-adjacent split mods detected
                    var baseModKey = entries.First(e => e.SplitIndex == null).ModKey;
                    var allModKeys = sortedByIndex.Select(e => e.ModKey).ToList();

                    return new SplitModsValidationResult(true, baseModKey, allModKeys);
                }
            }
        }

        return new SplitModsValidationResult(false, null, null);
    }
}
