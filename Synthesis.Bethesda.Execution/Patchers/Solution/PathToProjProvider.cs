using System.Diagnostics.CodeAnalysis;
using Noggog;

namespace Synthesis.Bethesda.Execution.Patchers.Solution
{
    public interface IPathToProjProvider
    {
        FilePath Path { get; }
    }

    public class PathToProjProvider : IPathToProjProvider
    {
        private readonly IProjectPathConstructor _pathConstructor;
        private readonly IPathToSolutionFileProvider _solutionPathProvider;
        private readonly IProjectSubpathProvider _subpathProvider;
        public FilePath Path => _pathConstructor.Construct(_solutionPathProvider.Path, _subpathProvider.ProjectSubpath);

        public PathToProjProvider(
            IProjectPathConstructor pathConstructor,
            IPathToSolutionFileProvider solutionPathProvider,
            IProjectSubpathProvider subpathProvider)
        {
            _pathConstructor = pathConstructor;
            _solutionPathProvider = solutionPathProvider;
            _subpathProvider = subpathProvider;
        }
    }

    [ExcludeFromCodeCoverage]
    public class PathToProjInjection : IPathToProjProvider
    {
        public FilePath Path { get; init; }
    }
}