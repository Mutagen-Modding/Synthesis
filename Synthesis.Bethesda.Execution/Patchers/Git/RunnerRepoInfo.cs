using System;
using System.Collections.Generic;
using System.Text;

namespace Synthesis.Bethesda.Execution.Patchers.Git
{
    public record RunnerRepoInfo(
        string SolutionPath,
        string ProjPath,
        string? Target,
        string CommitMessage,
        DateTime CommitDate,
        string? ListedMutagenVersion,
        string? ListedSynthesisVersion,
        string? TargetMutagenVersion,
        string? TargetSynthesisVersion);
}
