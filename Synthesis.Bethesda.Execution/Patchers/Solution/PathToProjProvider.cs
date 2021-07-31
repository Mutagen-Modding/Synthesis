using System.Diagnostics.CodeAnalysis;
using Noggog;

namespace Synthesis.Bethesda.Execution.Patchers.Solution
{
    public interface IPathToProjProvider
    {
        FilePath Path { get; }
    }

    [ExcludeFromCodeCoverage]
    public class PathToProjInjection : IPathToProjProvider
    {
        public FilePath Path { get; init; }
    }
}