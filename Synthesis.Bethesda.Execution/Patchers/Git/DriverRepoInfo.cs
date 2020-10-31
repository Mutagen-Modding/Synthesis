using System;
using System.Collections.Generic;
using System.Text;

namespace Synthesis.Bethesda.Execution.Patchers.Git
{
    public class DriverRepoInfo
    {
        public readonly string SolutionPath;
        public readonly List<(int Index, string Name)> Tags;
        public readonly List<string> AvailableProjects;
        public readonly string MasterBranchName;

        public DriverRepoInfo(
            string slnPath,
            string masterBranchName,
            List<(int Index, string Name)> tags,
            List<string> availableProjects)
        {
            SolutionPath = slnPath;
            Tags = tags;
            MasterBranchName = masterBranchName;
            AvailableProjects = availableProjects;
        }
    }
}
