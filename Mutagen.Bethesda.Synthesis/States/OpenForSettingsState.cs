using Synthesis.Bethesda;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Mutagen.Bethesda.Synthesis
{
    /// <summary>
    /// A class housing all the tools, parameters, and entry points to open for settings manipulation
    /// </summary>
    public class OpenForSettingsState : IOpenForSettingsState
    {
        /// <summary>
        /// Instructions given to the patcher from the Synthesis pipeline
        /// </summary>
        public OpenForSettings Settings { get; }

        /// <summary>
        /// A list of ModKeys as they appeared, and whether they were enabled
        /// </summary>
        public IReadOnlyList<LoadOrderListing> LoadOrder { get; }
        IEnumerable<LoadOrderListing> IOpenForSettingsState.LoadOrder => this.LoadOrder;

        public OpenForSettingsState(
            OpenForSettings settings,
            IReadOnlyList<LoadOrderListing> rawLoadOrder)
        {
            Settings = settings;
            LoadOrder = rawLoadOrder;
        }
    }
}
