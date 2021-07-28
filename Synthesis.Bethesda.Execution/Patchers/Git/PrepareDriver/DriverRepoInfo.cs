using System.Collections.Generic;
using Noggog;

namespace Synthesis.Bethesda.Execution.Patchers.Git.PrepareDriver
{
    public record DriverRepoInfo(
        FilePath SolutionPath,
        List<DriverTag> Tags,
        List<string> AvailableProjects,
        string MasterBranchName,
        Dictionary<string, string> BranchShas);
}
