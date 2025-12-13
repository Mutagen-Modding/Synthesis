using Noggog;
using Synthesis.Bethesda.Commands;

namespace Synthesis.Bethesda.Execution.Patchers.Running;

public interface IPatcherPrepForRun : IDisposableDropoff
{
    Task Prep(CancellationToken cancel);
}

public interface IPatcherRunExecution : IDisposableDropoff
{
    Guid Key { get; }
    int Index { get; }
    string Name { get; }
    Task Run(RunSynthesisPatcher settings, CancellationToken cancel);
}

public interface IPatcherPrepAndRun : IPatcherPrepForRun, IPatcherRunExecution
{
}