using Synthesis.Bethesda.Execution.Patchers.Git.Services;

namespace Synthesis.Bethesda.ImpactTester;

public class PipelineSettings : IShortCircuitSettingsProvider, IExecutionParametersSettingsProvider, IBlockBuildingWithinMo2SettingsProvider
{
    public bool Shortcircuit { get; set; }
    public string? TargetRuntime { get; set; } = "win-x64";
    public bool BlockBuildingWithinMo2 => false;
}