using Noggog;
using Synthesis.Bethesda.Commands;
using Synthesis.Bethesda.Execution.Utility;

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
    Task Run(RunSynthesisPatcher settings, PatcherRunCapture capture, CancellationToken cancel);
}

public interface IPatcherPrepAndRun : IPatcherPrepForRun, IPatcherRunExecution
{
}