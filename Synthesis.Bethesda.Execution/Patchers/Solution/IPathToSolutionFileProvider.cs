using Noggog;

namespace Synthesis.Bethesda.Execution.Patchers.Solution;

public interface IPathToSolutionFileProvider
{
    FilePath Path { get; }
}