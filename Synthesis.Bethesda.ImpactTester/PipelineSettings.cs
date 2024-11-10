using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Patchers.Git.Services;

namespace Synthesis.Bethesda.ImpactTester;

public class PipelineSettings : IShortCircuitSettingsProvider, IExecutionParametersSettingsProvider
{
    public bool Shortcircuit { get; set; }
    public string? TargetRuntime { get; set; } = "win-x64";
}