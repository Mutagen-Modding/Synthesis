using System.Diagnostics.CodeAnalysis;
using Mutagen.Bethesda.Plugins;

namespace Synthesis.Bethesda.Execution.Exceptions;

/// <summary>
/// Exception thrown when split mod files are not adjacent in the load order.
/// Split mods (e.g., Patch.esp, Patch_2.esp, Patch_3.esp) must be consecutive.
/// </summary>
[ExcludeFromCodeCoverage]
public class NonAdjacentSplitModsException : Exception
{
    public ModKey BaseModKey { get; }
    public IReadOnlyList<ModKey> SplitModKeys { get; }

    public NonAdjacentSplitModsException(ModKey baseModKey, IReadOnlyList<ModKey> splitModKeys)
        : base($"Split mods for '{baseModKey}' are not adjacent in the load order: {string.Join(", ", splitModKeys)}")
    {
        BaseModKey = baseModKey;
        SplitModKeys = splitModKeys;
    }
}
