namespace Synthesis.Bethesda.Execution.Patchers.Running.Git;

public record GitCompilationMeta
{
    public required string SynthesisUiVersion { get; init; }
    public required string NetSdkVersion { get; init; }
    public required string MutagenVersion { get; init; }
    public required string SynthesisVersion { get; init; }
    public required string Sha { get; init; }
    public bool DoesNotHaveRunnability { get; init; }
    public SettingsConfiguration? SettingsConfiguration { get; init; }
}