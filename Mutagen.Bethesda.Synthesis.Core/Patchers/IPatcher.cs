using Mutagen.Bethesda.Synthesis.Core.Settings;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mutagen.Bethesda.Synthesis.Core.Patchers
{
    public interface IPatcher : IDisposable
    {
        string Name { get; }
        Task Prep(CancellationToken? cancel = null);
        Task Run(ModPath? sourcePath, ModPath outputPath);
        Task Complete { get; }
    }
}
