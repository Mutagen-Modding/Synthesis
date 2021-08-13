using System;
using System.Threading;
using System.Threading.Tasks;
using Noggog;

namespace Synthesis.Bethesda.Execution.Patchers.Running
{
    public interface IPatcherRun : IDisposableDropoff
    {
        Guid Key { get; }
        int Index { get; }
        string Name { get; }
        Task Prep(CancellationToken cancel);
        Task Run(RunSynthesisPatcher settings, CancellationToken cancel);
    }
}
