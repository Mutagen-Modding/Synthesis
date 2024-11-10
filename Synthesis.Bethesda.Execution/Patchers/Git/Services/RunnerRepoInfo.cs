using System.Diagnostics.CodeAnalysis;
using Noggog;
using Synthesis.Bethesda.Execution.Patchers.Git.Services.PrepareRunner;
using Synthesis.Bethesda.Execution.Versioning;

namespace Synthesis.Bethesda.Execution.Patchers.Git.Services;

public record TargetProject(
    FilePath SolutionPath,
    FilePath ProjPath,
    string ProjSubPath);

[ExcludeFromCodeCoverage]
public record RunnerRepoInfo(
    TargetProject Project,
    RepoTarget Target,
    string CommitMessage,
    FilePath MetaPath,
    DateTime CommitDate,
    NugetVersionPair ListedVersions,
    NugetVersionPair TargetVersions);