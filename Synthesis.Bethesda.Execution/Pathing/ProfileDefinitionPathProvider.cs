using System.Diagnostics.CodeAnalysis;
using Noggog;

namespace Synthesis.Bethesda.Execution.Pathing
{
    public interface IProfileDefinitionPathProvider
    {
        FilePath Path { get; }
    }

    [ExcludeFromCodeCoverage]
    public class ProfileDefinitionPathInjection : IProfileDefinitionPathProvider
    {
        public FilePath Path { get; init; }
    }
}