namespace Synthesis.Bethesda.Execution.Patchers.Git;

public interface IGithubPatcherIdentifier
{
    string Id { get; }
}

public record GithubPatcherIdentifier(string Id) : IGithubPatcherIdentifier;