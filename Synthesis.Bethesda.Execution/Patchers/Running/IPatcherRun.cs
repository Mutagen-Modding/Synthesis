using System;
using System.Threading;
using System.Threading.Tasks;
using Mutagen.Bethesda;
using Noggog;

namespace Synthesis.Bethesda.Execution.Patchers.Running
{
    public interface IPatcherRun : IDisposableDropoff
    {
        string Name { get; }
        Task Prep(CancellationToken cancel);
        Task Run(RunSynthesisPatcher settings, CancellationToken cancel);
    }
}
