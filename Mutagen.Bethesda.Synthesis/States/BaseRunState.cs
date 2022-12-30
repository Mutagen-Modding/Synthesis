using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Plugins.Records;
using Noggog;

namespace Mutagen.Bethesda.Synthesis;

public interface IBaseRunState : ISettingsDataConfiguration
{
    /// <summary>
    /// A list of ModKeys as they appeared, and whether they were enabled
    /// </summary>
    IReadOnlyList<ILoadOrderListingGetter> RawLoadOrder { get; }

    /// <summary>
    /// Path to the plugins.txt used
    /// </summary>
    FilePath LoadOrderFilePath { get; }

    /// <summary>
    /// Path to the game data folder
    /// </summary>
    DirectoryPath DataFolderPath { get; }

    /// <summary>
    /// GameRelease targeted for patching
    /// </summary>
    GameRelease GameRelease { get; }
}

public interface IBaseRunState<TModSetter, TModGetter> : IBaseRunState
    where TModSetter : class, IMod, TModGetter
    where TModGetter : class, IModGetter
{
    /// <summary>
    /// Load Order object containing all the mods to be used for the patch.<br />
    /// This Load Order will contain the patch mod itself.  This reference is the same object
    /// as the PatchMod member, and so any modifications will implicitly be applied to the Load Order.
    /// </summary>
    ILoadOrder<IModListing<TModGetter>> LoadOrder { get; }

    /// <summary>
    /// Convenience Link Cache to use created from the provided Load Order object.<br />
    /// The patch mod is marked as safe for mutation, and will not make the cache invalid.
    /// </summary>
    ILinkCache<TModSetter, TModGetter> LinkCache { get; }
}