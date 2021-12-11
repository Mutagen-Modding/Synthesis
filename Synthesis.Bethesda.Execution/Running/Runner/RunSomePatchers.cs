using System.Threading;
using System.Threading.Tasks;
using Mutagen.Bethesda.Plugins;
using Noggog;

namespace Synthesis.Bethesda.Execution.Running.Runner
{
    public interface IRunSomePatchers
    {
        Task<FilePath?> Run(
            ModKey outputKey,
            PatcherPrepBundle[] patchers,
            CancellationToken cancellation,
            FilePath? sourcePath,
            RunParameters runParameters);
    }

    public class RunSomePatchers : IRunSomePatchers
    {
        public IRunAPatcher RunAPatcher { get; }

        public RunSomePatchers(IRunAPatcher runPatcher)
        {
            RunAPatcher = runPatcher;
        }
        
        public async Task<FilePath?> Run(
            ModKey outputKey,
            PatcherPrepBundle[] patchers,
            CancellationToken cancellation,
            FilePath? sourcePath,
            RunParameters runParameters)
        {
            for (int i = 0; i < patchers.Length; i++)
            {
                var patcher = patchers[i];

                var nextPath = await RunAPatcher.Run(
                    outputKey: outputKey,
                    patcher,
                    cancellation,
                    sourcePath,
                    runParameters).ConfigureAwait(false);

                if (nextPath == null) return null;
                
                sourcePath = nextPath;
            }

            return sourcePath;
        }
    }
}