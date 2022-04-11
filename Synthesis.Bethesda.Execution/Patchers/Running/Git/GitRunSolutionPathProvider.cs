using System.IO;
using Noggog;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Patchers.Solution;

namespace Synthesis.Bethesda.Execution.Patchers.Running.Git;

public class GitRunSolutionPathProvider : IPathToSolutionFileProvider
{
    public ISolutionFileLocator SolutionFileLocator { get; }
    public IRunnerRepoDirectoryProvider RunnerRepoDirectoryProvider { get; }
    public FilePath Path => SolutionFileLocator.GetPath(RunnerRepoDirectoryProvider.Path) ?? throw new FileNotFoundException("Could not find solution path within repository");

    public GitRunSolutionPathProvider(
        ISolutionFileLocator solutionFileLocator,
        IRunnerRepoDirectoryProvider runnerRepoDirectoryProvider)
    {
        SolutionFileLocator = solutionFileLocator;
        RunnerRepoDirectoryProvider = runnerRepoDirectoryProvider;
    }
}