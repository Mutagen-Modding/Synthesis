using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mutagen.Bethesda.Synthesis;

public class RunPreferences
{
    /// <summary>
    /// If program args are empty, what actions should be taken
    /// </summary>
    public RunDefaultPatcher? ActionsForEmptyArgs { get; set; } = null;

    /// <summary>
    /// Whether to forcibly shut down the app after handling arguments,
    /// if the arguments were not to open for settings.
    /// </summary>
    public bool ForceShutdownIfNotOpeningForSettings = true;
}