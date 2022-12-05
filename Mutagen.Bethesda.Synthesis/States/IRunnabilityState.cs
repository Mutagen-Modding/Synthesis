using System.ComponentModel;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Synthesis.States;
using Synthesis.Bethesda.Commands;

namespace Mutagen.Bethesda.Synthesis;

/// <summary>
/// An interface housing all the tools, parameters, and entry points for checking if a patcher is runnable
/// </summary>
public interface IRunnabilityState : IEnvironmentCreationState
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    CheckRunnability Settings { get; }

    /// <summary>
    /// A list of ModKeys as they appeared, and whether they were enabled
    /// </summary>
    ILoadOrderGetter<ILoadOrderListingGetter> LoadOrder { get; }
}