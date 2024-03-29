using LibGit2Sharp;

namespace Synthesis.Bethesda.Execution;

public static class RepositoryExtensions
{
    public static void Fetch(this Repository repo)
    {
        var fetchOptions = new FetchOptions();
        foreach (var remote in repo.Network.Remotes)
        {
            var refSpecs = remote.FetchRefSpecs.Select(x => x.Specification);
            LibGit2Sharp.Commands.Fetch(repo, remote.Name, refSpecs, fetchOptions, string.Empty);
        }
    }
}