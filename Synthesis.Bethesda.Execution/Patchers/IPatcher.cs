using Mutagen.Bethesda;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Synthesis.Bethesda.Execution.Patchers
{
    public interface IPatcher : IDisposable
    {
        string Name { get; }
        Task Prep(CancellationToken? cancel = null);
        Task Run(ModPath? sourcePath, ModPath outputPath);
    }
}
