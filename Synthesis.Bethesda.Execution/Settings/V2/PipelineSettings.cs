using System.Diagnostics.CodeAnalysis;

namespace Synthesis.Bethesda.Execution.Settings.V2;

[ExcludeFromCodeCoverage]
public record PipelineSettings : IPipelineSettings
{
    public int Version => 2; 
    public IList<ISynthesisProfileSettings> Profiles { get; set; } = new List<ISynthesisProfileSettings>();
    public bool Shortcircuit { get; set; } = true;
    public string WorkingDirectory { get; set; } = string.Empty;
    public double BuildCorePercentage { get; set; } = 0.5d;
    public string DotNetPathOverride { get; set; } = string.Empty;
}