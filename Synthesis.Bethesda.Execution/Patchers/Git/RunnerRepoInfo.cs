using System;
using System.Collections.Generic;
using System.Text;

namespace Synthesis.Bethesda.Execution.Patchers.Git
{
    public class RunnerRepoInfo
    {
        public readonly string SolutionPath;
        public readonly string ProjPath;
        public readonly string? Target;
        public readonly string CommitMessage;
        public readonly DateTime CommitDate;
        public readonly string? ListedMutagenVersion;
        public readonly string? ListedSynthesisVersion;

        public RunnerRepoInfo(
            string slnPath,
            string projPath,
            string? target,
            string commitMsg,
            DateTime commitDate,
            string? listedSynthesis,
            string? listedMutagen)
        {
            SolutionPath = slnPath;
            ProjPath = projPath;
            Target = target;
            CommitMessage = commitMsg;
            CommitDate = commitDate;
            ListedMutagenVersion = listedMutagen;
            ListedSynthesisVersion = listedSynthesis;
        }
    }
}
