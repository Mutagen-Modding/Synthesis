using Noggog;
using Synthesis.Bethesda.Commands;

namespace Synthesis.Bethesda.Execution.Patchers.Running;

public interface IPatcherRun : IDisposableDropoff
{
    Guid Key { get; }
    int Index { get; }
    string Name { get; }
    Task Prep(CancellationToken cancel);
    Task Run(RunSynthesisPatcher settings, CancellationToken cancel);
}