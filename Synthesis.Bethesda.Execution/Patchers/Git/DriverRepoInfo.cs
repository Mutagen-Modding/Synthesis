using System;
using System.Collections.Generic;
using System.Text;
using Noggog;

namespace Synthesis.Bethesda.Execution.Patchers.Git
{
    public class DriverRepoInfo
    {
        public readonly FilePath SolutionPath;
        public readonly List<(int Index, string Name, string Sha)> Tags;
        public readonly List<string> AvailableProjects;
        public readonly string MasterBranchName;
        public readonly Dictionary<string, string> BranchShas;

        public DriverRepoInfo(
            FilePath slnPath,
            string masterBranchName,
            Dictionary<string, string> branchShas,
            List<(int Index, string Name, string Sha)> tags,
            List<string> availableProjects)
        {
            SolutionPath = slnPath;
            Tags = tags;
            BranchShas = branchShas;
            MasterBranchName = masterBranchName;
            AvailableProjects = availableProjects;
        }
    }
}
