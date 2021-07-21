using System;
using System.Threading;
using System.Threading.Tasks;
using Mutagen.Bethesda;

namespace Synthesis.Bethesda.Execution.Patchers.Running
{
    public interface IPatcherRun : IDisposable
    {
        string Name { get; }
        Task Prep(GameRelease release, CancellationToken cancel);
        Task Run(RunSynthesisPatcher settings, CancellationToken cancel);
        IObservable<string> Output { get; }
        IObservable<string> Error { get; }
        public void AddForDisposal(IDisposable disposable);
    }
}