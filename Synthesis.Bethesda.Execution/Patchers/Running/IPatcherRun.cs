using System;
using System.Threading;
using System.Threading.Tasks;
using Mutagen.Bethesda;

namespace Synthesis.Bethesda.Execution.Patchers.Running
{
    public interface IPatcherRun : IDisposable
    {
        string Name { get; }
        Task Prep(CancellationToken cancel);
        Task Run(RunSynthesisPatcher settings, CancellationToken cancel);
        public void AddForDisposal(IDisposable disposable);
    }
}
