using System.Collections.Generic;

namespace Synthesis.Bethesda.Execution.Settings
{
    public interface IPatcherGroup
    {
        List<PatcherSettings> Patchers { get; set; }
    }
}