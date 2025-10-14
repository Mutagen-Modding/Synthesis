using Synthesis.Bethesda.Execution.Patchers.Git.Services;

namespace Synthesis.Bethesda.ImpactTester;

public class PipelineSettings : IShortCircuitSettingsProvider, IExecutionParametersSettingsProvider, IMo2CompatibilitySettingsProvider
{
    public bool Shortcircuit { get; set; }
    public string? TargetRuntime { get; set; } = "win-x64";
    public bool Mo2Compatibility { get; set; } = true;
}