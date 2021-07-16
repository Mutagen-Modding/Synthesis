using Noggog;

namespace Synthesis.Bethesda.Execution.Pathing
{
    public interface IProfileDefinitionPathProvider
    {
        FilePath Path { get; }
    }

    public class ProfileDefinitionPathInjection : IProfileDefinitionPathProvider
    {
        public FilePath Path { get; init; }
    }
}