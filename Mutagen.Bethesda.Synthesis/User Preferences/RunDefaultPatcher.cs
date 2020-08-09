using System;
using System.Collections.Generic;
using System.Text;

namespace Mutagen.Bethesda.Synthesis
{
    public class RunDefaultPatcher
    {
        /// <summary>
        /// ModKey to export to the data folder under
        /// </summary>
        public ModKey IdentifyingModKey { get; set; }

        /// <summary>
        /// GameRelease to target
        /// </summary>
        public GameRelease TargetRelease { get; set; }
    }
}
