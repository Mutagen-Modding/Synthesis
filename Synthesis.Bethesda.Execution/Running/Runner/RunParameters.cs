using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.Execution.Running.Runner
{
    public record RunParameters(
        PersistenceMode PersistenceMode,
        string? PersistencePath);
}