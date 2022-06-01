using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Plugins.Records;
using Noggog;
using System.ComponentModel;
using Mutagen.Bethesda.Environments;
using Synthesis.Bethesda.Commands;

namespace Mutagen.Bethesda.Synthesis;

/// <summary>
/// An interface housing all the tools, parameters, and entry points for checking if a patcher is runnable
/// </summary>
public interface IRunnabilityState
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    CheckRunnability Settings { get; }

    /// <summary>
    /// A list of ModKeys as they appeared, and whether they were enabled
    /// </summary>
    ILoadOrderGetter<ILoadOrderListingGetter> LoadOrder { get; }

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

    GameEnvironmentState<TModSetter, TModGetter> GetEnvironmentState<TModSetter, TModGetter>()
        where TModSetter : class, IContextMod<TModSetter, TModGetter>, TModGetter
        where TModGetter : class, IContextGetterMod<TModSetter, TModGetter>;
}