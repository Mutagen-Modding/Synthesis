using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Analysis;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Plugins.Order.DI;
using Noggog;
using Serilog;

namespace Synthesis.Bethesda.Execution.Running.Runner;

public interface ILoadOrderMaintainer
{
    void UpdateLoadOrder(ModKey baseModKey, IReadOnlyList<ModKey> outputModKeys);
}

public class LoadOrderMaintainer : ILoadOrderMaintainer
{
    private readonly ILogger _logger;
    private readonly IPluginListingsPathContext _pluginListingsPathContext;
    private readonly IPluginListingsProvider _pluginListingsProvider;
    private readonly ILoadOrderWriter _loadOrderWriter;

    public LoadOrderMaintainer(
        ILogger logger,
        IPluginListingsPathContext pluginListingsPathContext,
        IPluginListingsProvider pluginListingsProvider,
        ILoadOrderWriter loadOrderWriter)
    {
        _logger = logger;
        _pluginListingsPathContext = pluginListingsPathContext;
        _pluginListingsProvider = pluginListingsProvider;
        _loadOrderWriter = loadOrderWriter;
    }

    public void UpdateLoadOrder(ModKey baseModKey, IReadOnlyList<ModKey> outputModKeys)
    {
        if (outputModKeys.Count == 0)
        {
            throw new InvalidOperationException(
                $"No output mod keys provided for {baseModKey}");
        }

        // 1. Read current load order
        var currentLoadOrder = _pluginListingsProvider.Get().ToList();

        // 2. Find existing entries (base + splits) using MultiModFileAnalysis.IsSplitModSibling
        var existingEntries = new List<(int Index, ILoadOrderListingGetter Listing)>();
        for (int i = 0; i < currentLoadOrder.Count; i++)
        {
            var listing = currentLoadOrder[i];
            if (listing.ModKey == baseModKey || MultiModFileAnalysis.IsSplitModSibling(listing.ModKey, baseModKey))
            {
                existingEntries.Add((i, listing));
            }
        }

        // 3. Record insertion position and enabled state
        int insertPosition;
        bool enabled;

        if (existingEntries.Count > 0)
        {
            // Use first entry's position
            insertPosition = existingEntries.Min(e => e.Index);

            // Use base mod's enabled state if found, otherwise use first entry's state
            var baseEntry = existingEntries.FirstOrDefault(e => e.Listing.ModKey == baseModKey);
            enabled = baseEntry.Listing?.Enabled ?? existingEntries.First().Listing.Enabled;
        }
        else
        {
            // No existing entries - append at end
            insertPosition = currentLoadOrder.Count;
            enabled = true;
        }

        // 4. Build new load order: remove old entries, insert new entries at position
        var removedEntries = existingEntries.Select(e => e.Listing.ModKey).ToList();

        // Create new load order list
        var newLoadOrder = new List<ILoadOrderListingGetter>();
        int currentIndex = 0;
        int adjustedInsertPosition = insertPosition;

        // Add entries before insert position, skipping removed ones
        for (int i = 0; i < currentLoadOrder.Count; i++)
        {
            if (existingEntries.Any(e => e.Index == i))
            {
                // Skip removed entries
                if (i < insertPosition)
                {
                    adjustedInsertPosition--;
                }
                continue;
            }

            if (currentIndex == adjustedInsertPosition)
            {
                // Insert new entries at this position
                foreach (var modKey in outputModKeys)
                {
                    newLoadOrder.Add(new LoadOrderListing(modKey, enabled));
                }
            }

            newLoadOrder.Add(currentLoadOrder[i]);
            currentIndex++;
        }

        // If we haven't inserted yet (insert at end), do it now
        if (adjustedInsertPosition >= currentIndex)
        {
            foreach (var modKey in outputModKeys)
            {
                newLoadOrder.Add(new LoadOrderListing(modKey, enabled));
            }
        }

        // 5. Write back using ILoadOrderWriter
        var pluginsPath = _pluginListingsPathContext.Path;
        _loadOrderWriter.Write(pluginsPath, newLoadOrder, removeImplicitMods: false);

        _logger.Information("Updated load order: removed {RemovedCount} entries, added {AddedCount} entries at position {Position}",
            removedEntries.Count, outputModKeys.Count, insertPosition);

        foreach (var mod in outputModKeys)
        {
            _logger.Information("  Added: {ModKey} (enabled: {Enabled})", mod, enabled);
        }
    }
}
