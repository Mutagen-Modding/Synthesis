using System.Collections.Generic;

namespace Synthesis.Bethesda.Execution.Settings
{
    public interface IPipelineSettings
    {
        IList<ISynthesisProfileSettings> Profiles { get; set; }
    }
}