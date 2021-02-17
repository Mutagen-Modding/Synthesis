using System;
using System.Collections.Generic;
using System.Text;

namespace Synthesis.Bethesda.Execution.Patchers.Git
{
    public record DriverRepoInfo(
        string SolutionPath,
        string MasterBranchName,
        Dictionary<string, string> BranchShas,
        List<(int Index, string Name, string Sha)> Tags,
        List<string> AvailableProjects);
}
