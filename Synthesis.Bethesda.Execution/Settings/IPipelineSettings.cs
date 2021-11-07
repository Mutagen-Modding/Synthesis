using System.Collections.Generic;
using Synthesis.Bethesda.Execution.Patchers.Running.Git;

namespace Synthesis.Bethesda.Execution.Settings
{
    public interface IPipelineSettings : IShortCircuitCompilationSettingsProvider
    {
        IList<ISynthesisProfileSettings> Profiles { get; set; }
    }
}