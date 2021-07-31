using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Synthesis.Bethesda.Execution.Patchers.Git
{
    [ExcludeFromCodeCoverage]
    public record RunnerRepoInfo(
        string SolutionPath,
        string ProjPath,
        string? Target,
        string CommitMessage,
        DateTime CommitDate,
        NugetVersionPair ListedVersions,
        NugetVersionPair TargetVersions);
}
