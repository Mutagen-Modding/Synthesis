using Noggog;
using Synthesis.Bethesda.Execution.GitRepository;

namespace Synthesis.Bethesda.Execution.Patchers.Git.PrepareRunner;

public interface IRetrieveCommit
{
    GetResponse<ICommit> TryGet(
        IGitRepository repo,
        RepoTarget targets,
        GitPatcherVersioning patcherVersioning,
        CancellationToken cancel);
}

public class RetrieveCommit : IRetrieveCommit
{
    public IShouldFetchIfMissing ShouldFetchIfMissing { get; }

    public RetrieveCommit(
        IShouldFetchIfMissing shouldFetchIfMissing)
    {
        ShouldFetchIfMissing = shouldFetchIfMissing;
    }
        
    public GetResponse<ICommit> TryGet(
        IGitRepository repo,
        RepoTarget targets,
        GitPatcherVersioning patcherVersioning,
        CancellationToken cancel)
    {
        var commit = repo.TryGetCommit(targets.TargetSha, out var validSha);
        if (!validSha)
        {
            return GetResponse<ICommit>.Fail("Malformed sha string");
        }

        cancel.ThrowIfCancellationRequested();
        if (commit == null)
        {
            bool fetchIfMissing = ShouldFetchIfMissing.Should(patcherVersioning);
            if (!fetchIfMissing)
            {
                return GetResponse<ICommit>.Fail("Could not locate commit with given sha");
            }
            repo.Fetch();
            commit = repo.TryGetCommit(targets.TargetSha, out _);
            if (commit == null)
            {
                return GetResponse<ICommit>.Fail("Could not locate commit with given sha");
            }
        }

        return GetResponse<ICommit>.Succeed(commit);
    }
}