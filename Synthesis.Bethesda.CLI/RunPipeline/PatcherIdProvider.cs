using Synthesis.Bethesda.Execution.Patchers.Common;

namespace Synthesis.Bethesda.CLI.RunPipeline;

public class PatcherIdProvider : IPatcherIdProvider
{
    public Guid InternalId { get; } = Guid.NewGuid();
}