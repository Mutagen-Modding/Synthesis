using Synthesis.Bethesda.Execution.Patchers.Git.Services;

namespace Synthesis.Bethesda.Execution.Settings;

public interface IPipelineSettings : IShortCircuitSettingsProvider
{
    List<ISynthesisProfileSettings> Profiles { get; set; }
    string WorkingDirectory { get; set; }
    double BuildCorePercentage { get; set; }
    string DotNetPathOverride { get; set; }
    bool Mo2Compatibility { get; set; }
}