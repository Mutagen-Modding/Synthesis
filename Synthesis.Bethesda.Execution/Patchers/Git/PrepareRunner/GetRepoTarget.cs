using System;
using Noggog;
using Synthesis.Bethesda.Execution.GitRepository;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.Execution.Patchers.Git.PrepareRunner
{
    public record RepoTarget(string TargetSha, string Target);

    public interface IGetRepoTarget
    {
        GetResponse<RepoTarget> Get(
            IGitRepository repo,
            GitPatcherVersioning patcherVersioning);
    }

    public class GetRepoTarget : IGetRepoTarget
    {
        public GetResponse<RepoTarget> Get(
            IGitRepository repo,
            GitPatcherVersioning patcherVersioning)
        {
            string? targetSha;
            string? target;
            switch (patcherVersioning.Versioning)
            {
                case PatcherVersioningEnum.Tag:
                    if (string.IsNullOrWhiteSpace(patcherVersioning.Target)) return GetResponse<RepoTarget>.Fail("No tag selected");
                    repo.Fetch();
                    if (!repo.TryGetTagSha(patcherVersioning.Target, out targetSha)) return GetResponse<RepoTarget>.Fail("Could not locate tag");
                    target = patcherVersioning.Target;
                    break;
                case PatcherVersioningEnum.Commit:
                    targetSha = patcherVersioning.Target;
                    if (string.IsNullOrWhiteSpace(targetSha)) return GetResponse<RepoTarget>.Fail("Could not locate commit");
                    target = patcherVersioning.Target;
                    break;
                case PatcherVersioningEnum.Branch:
                    if (string.IsNullOrWhiteSpace(patcherVersioning.Target)) return GetResponse<RepoTarget>.Fail($"Target branch had no name.");
                    repo.Fetch();
                    if (!repo.TryGetBranch(patcherVersioning.Target, out var targetBranch)) return GetResponse<RepoTarget>.Fail($"Could not locate branch: {patcherVersioning.Target}");
                    targetSha = targetBranch.Tip.Sha;
                    target = patcherVersioning.Target;
                    break;
                default:
                    throw new NotImplementedException();
            }
            return GetResponse<RepoTarget>.Succeed(
                new RepoTarget(targetSha, target));
        }
    }
}