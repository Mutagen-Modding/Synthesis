using Synthesis.Bethesda.Execution.Patchers.Git;

namespace Synthesis.Bethesda.ImpactTester;

public class PipelineSettings : IShortCircuitSettingsProvider, IExecutionParametersSettingsProvider
{
    public bool Shortcircuit { get; set; }
    public bool SpecifyTargetFramework { get; set; } = true;
}