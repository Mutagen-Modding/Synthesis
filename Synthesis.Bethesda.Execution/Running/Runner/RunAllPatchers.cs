using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mutagen.Bethesda.Plugins;
using Noggog;

namespace Synthesis.Bethesda.Execution.Running.Runner
{
    public interface IRunAllPatchers
    {
        Task<FilePath?> Run(
            ModKey outputKey,
            IEnumerable<PatcherPrepBundle> patchers,
            CancellationToken cancellation,
            FilePath? sourcePath = null,
            string? persistencePath = null);
    }

    public class RunAllPatchers : IRunAllPatchers
    {
        public IRunAPatcher RunAPatcher { get; }

        public RunAllPatchers(IRunAPatcher runAPatcher)
        {
            RunAPatcher = runAPatcher;
        }

        public async Task<FilePath?> Run(
            ModKey outputKey,
            IEnumerable<PatcherPrepBundle> patchers,
            CancellationToken cancellation,
            FilePath? sourcePath = null,
            string? persistencePath = null)
        {
            foreach (var patcher in patchers)
            {
                var nextPath = await RunAPatcher.Run(
                    outputKey: outputKey,
                    patcher,
                    cancellation,
                    sourcePath,
                    persistencePath);

                if (nextPath == null) return null;
                
                sourcePath = nextPath;
            }

            return sourcePath;
        }
    }
}