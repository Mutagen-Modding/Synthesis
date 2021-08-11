using System.Collections.Generic;

namespace Synthesis.Bethesda.Execution.Settings.V2
{
    public class PatcherGroup : IPatcherGroup
    {
        public List<PatcherSettings> Patchers { get; set; } = new();
    }
}