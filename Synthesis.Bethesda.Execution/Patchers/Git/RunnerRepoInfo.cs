using System;
using System.Diagnostics.CodeAnalysis;
using Synthesis.Bethesda.Execution.Patchers.Git.PrepareRunner;
using Synthesis.Bethesda.Execution.Versioning;

namespace Synthesis.Bethesda.Execution.Patchers.Git
{
    [ExcludeFromCodeCoverage]
    public record RunnerRepoInfo(
        string SolutionPath,
        string ProjPath,
        RepoTarget Target,
        string CommitMessage,
        DateTime CommitDate,
        NugetVersionPair ListedVersions,
        NugetVersionPair TargetVersions);
}
