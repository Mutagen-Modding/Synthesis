using System;
using System.Threading;
using System.Threading.Tasks;
using Mutagen.Bethesda.Plugins;
using Noggog;
using Synthesis.Bethesda.Execution.Patchers.Running;

namespace Synthesis.Bethesda.Execution.Running.Runner
{
    public interface IRunAllPatchers
    {
        Task<FilePath?> Run(
            ModKey outputKey,
            (int Key, IPatcherRun Run)[] patchers,
            Task<Exception?>[] patcherPreps,
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
            (int Key, IPatcherRun Run)[] patchers,
            Task<Exception?>[] patcherPreps,
            CancellationToken cancellation,
            FilePath? sourcePath = null,
            string? persistencePath = null)
        {
            for (int i = 0; i < patchers.Length; i++)
            {
                var patcher = patchers[i];

                var nextPath = await RunAPatcher.Run(
                    outputKey: outputKey,
                    patcher.Key,
                    patcher.Run,
                    patcherPreps[i],
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