using System.Diagnostics.CodeAnalysis;
using Noggog;

namespace Synthesis.Bethesda.Execution.Patchers.Git.Services.PrepareDriver;

[ExcludeFromCodeCoverage]
public record DriverRepoInfo(
    FilePath SolutionPath,
    List<DriverTag> Tags,
    List<string> AvailableProjects,
    string MasterBranchName,
    Dictionary<string, string> BranchShas);