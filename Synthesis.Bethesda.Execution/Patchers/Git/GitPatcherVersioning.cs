using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.Execution.Patchers.Git;

public record GitPatcherVersioning(PatcherVersioningEnum Versioning, string Target)
{
    public override string ToString()
    {
        return Versioning switch
        {
            PatcherVersioningEnum.Tag => $"tag {Target}",
            PatcherVersioningEnum.Branch => $"branch {Target}",
            PatcherVersioningEnum.Commit => $"commit {Target}",
            _ => throw new NotImplementedException(),
        };
    }

    public static GitPatcherVersioning Factory(GithubPatcherSettings patcherSettings)
    {
        return Factory(
            versioning: patcherSettings.PatcherVersioning,
            tag: patcherSettings.TargetTag,
            commit: patcherSettings.TargetCommit,
            branch: patcherSettings.TargetBranch,
            autoTag: patcherSettings.LatestTag,
            autoBranch: patcherSettings.AutoUpdateToBranchTip);
    }

    public static GitPatcherVersioning Factory(
        PatcherVersioningEnum versioning,
        string tag,
        string commit,
        string branch,
        bool autoTag,
        bool autoBranch)
    {
        switch (versioning)
        {
            case PatcherVersioningEnum.Tag:
                if (!autoTag)
                {
                    versioning = PatcherVersioningEnum.Commit;
                }
                break;
            case PatcherVersioningEnum.Branch:
                if (!autoBranch)
                {
                    versioning = PatcherVersioningEnum.Commit;
                }
                break;
            default:
                break;
        }
        return new GitPatcherVersioning(
            versioning,
            versioning switch
            {
                PatcherVersioningEnum.Branch => branch,
                PatcherVersioningEnum.Tag => tag,
                PatcherVersioningEnum.Commit => commit,
                _ => throw new NotImplementedException(),
            });
    }
}