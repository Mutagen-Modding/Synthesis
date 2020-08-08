using System;
using System.Collections.Generic;
using System.Text;

namespace Mutagen.Bethesda.Synthesis
{
    public class UserPreferences
    {
        /// <summary>
        /// An enumerable of ModKeys to allow into the patcher.<br />
        /// Any mod not on the list will not be present in the supplied load order<br />
        /// Null means to include all mods
        /// </summary>
        public IEnumerable<ModKey>? InclusionMods;

        /// <summary>
        /// An enumerable of ModKeys to disallow into the patcher.<br />
        /// Any mod on the list will not be present in the supplied load order<br />
        /// Null means to include all mods
        /// </summary>
        public IEnumerable<ModKey>? ExclusionMods;
    }
}
