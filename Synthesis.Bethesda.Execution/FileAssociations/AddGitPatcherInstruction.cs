namespace Synthesis.Bethesda.Execution.FileAssociations;

public record AddGitPatcherInstruction
{
    public string Url { get; init; } = string.Empty;
    public string SelectedProject { get; init; } = string.Empty;
} 