using Noggog;

namespace Synthesis.Bethesda.Execution.Patchers.Cli
{
    public interface IPathToExecutableInputProvider
    {
        public FilePath Path { get; }
    }
}