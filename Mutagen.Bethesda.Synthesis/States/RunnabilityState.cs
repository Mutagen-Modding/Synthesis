using Synthesis.Bethesda;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Mutagen.Bethesda.Synthesis
{
    /// <summary>
    /// A class housing all the tools, parameters, and entry points for a typical Synthesis check runnability analysis
    /// </summary>
    public class RunnabilityState : IRunnabilityState
    {
        /// <summary>
        /// Instructions given to the patcher from the Synthesis pipeline
        /// </summary>
        public CheckRunnability Settings { get; }

        /// <summary>
        /// A list of ModKeys as they appeared, and whether they were enabled
        /// </summary>
        public IReadOnlyList<LoadOrderListing> LoadOrder { get; }
        IEnumerable<LoadOrderListing> IRunnabilityState.LoadOrder => this.LoadOrder;

        public RunnabilityState(
            CheckRunnability settings,
            IReadOnlyList<LoadOrderListing> rawLoadOrder)
        {
            Settings = settings;
            LoadOrder = rawLoadOrder;
        }
    }
}
