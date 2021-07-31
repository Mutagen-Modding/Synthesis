using System.Diagnostics.CodeAnalysis;
using Noggog;

namespace Synthesis.Bethesda.Execution.Patchers.Solution
{
    public interface IPathToSolutionFileProvider
    {
        FilePath Path { get; }
    }

    [ExcludeFromCodeCoverage]
    public class PathToSolutionFileInjection : IPathToSolutionFileProvider
    {
        public FilePath Path { get; init; }
    }
}