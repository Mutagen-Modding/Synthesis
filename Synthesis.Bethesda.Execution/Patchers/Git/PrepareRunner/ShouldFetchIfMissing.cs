using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.Execution.Patchers.Git.PrepareRunner
{
    public interface IShouldFetchIfMissing
    {
        bool Should(GitPatcherVersioning patcherVersioning);
    }

    public class ShouldFetchIfMissing : IShouldFetchIfMissing
    {
        public bool Should(GitPatcherVersioning patcherVersioning)
        {
            return patcherVersioning.Versioning switch
            {
                PatcherVersioningEnum.Commit => true,
                _ => false
            };
        }
    }
}