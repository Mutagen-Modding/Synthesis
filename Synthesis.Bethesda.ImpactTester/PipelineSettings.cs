using Synthesis.Bethesda.Execution.Patchers.Git;

namespace Synthesis.Bethesda.ImpactTester;

public class PipelineSettings : IShortCircuitSettingsProvider, IExecutionParametersSettingsProvider
{
    public bool Shortcircuit { get; set; }
    public string? TargetRuntime { get; set; } = "win-x64";
}