using System.Diagnostics.CodeAnalysis;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.Execution.Patchers.Git.Services.PrepareRunner;

public interface IShouldFetchIfMissing
{
    bool Should(GitPatcherVersioning patcherVersioning);
}

[ExcludeFromCodeCoverage]
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