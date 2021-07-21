using Noggog;

namespace Synthesis.Bethesda.Execution.Patchers.Solution
{
    public interface IPathToSolutionFileProvider
    {
        FilePath Path { get; }
    }

    public class PathToSolutionFileProvider : IPathToSolutionFileProvider
    {
        public FilePath Path { get; init; }
    }
}