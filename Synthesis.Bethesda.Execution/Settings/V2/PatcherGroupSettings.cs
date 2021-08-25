using System.Collections.Generic;
using Mutagen.Bethesda.Plugins;

namespace Synthesis.Bethesda.Execution.Settings.V2
{
    public class PatcherGroupSettings
    {
        public string Name { get; set; } = string.Empty;
        public bool On { get; set; }
        public ModKey ModKey { get; set; }
        public List<PatcherSettings> Patchers { get; set; } = new();
        
        
    }
}