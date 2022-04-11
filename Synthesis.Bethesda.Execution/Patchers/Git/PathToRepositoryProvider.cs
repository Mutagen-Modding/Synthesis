using Noggog;
using Synthesis.Bethesda.Execution.Patchers.Solution;

namespace Synthesis.Bethesda.Execution.Patchers.Git;
// ToDo
// Upgrade to support deep solutions

public interface IPathToRepositoryProvider
{
    DirectoryPath? Path { get; }
}

public class PathToRepositoryProvider : IPathToRepositoryProvider
{
    private readonly IPathToSolutionFileProvider _pathToSln;
    public DirectoryPath? Path => _pathToSln.Path.Directory;

    public PathToRepositoryProvider(
        IPathToSolutionFileProvider pathToSln)
    {
        _pathToSln = pathToSln;
    }
}