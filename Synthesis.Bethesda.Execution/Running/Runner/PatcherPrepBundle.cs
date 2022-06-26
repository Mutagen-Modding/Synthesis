using Synthesis.Bethesda.Execution.Patchers.Running;

namespace Synthesis.Bethesda.Execution.Running.Runner;

public record PatcherPrepBundle(IPatcherRun Run, Task<Exception?> Prep);