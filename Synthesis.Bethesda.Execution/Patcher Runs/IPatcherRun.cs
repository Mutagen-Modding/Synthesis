using Mutagen.Bethesda;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Synthesis.Bethesda.Execution.Patchers
{
    public interface IPatcherRun : IDisposable
    {
        string Name { get; }
        Task Prep(GameRelease release, CancellationToken? cancel = null);
        Task Run(RunSynthesisPatcher settings, CancellationToken? cancel = null);
        IObservable<string> Output { get; }
        IObservable<string> Error { get; }
    }
}
