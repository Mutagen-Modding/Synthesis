using System.Collections.Generic;
using Synthesis.Bethesda.Execution.Patchers.Git;

namespace Synthesis.Bethesda.Execution.Settings
{
    public interface IPipelineSettings : IShortCircuitSettingsProvider
    {
        IList<ISynthesisProfileSettings> Profiles { get; set; }
    }
}