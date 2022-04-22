using Noggog;
using Synthesis.Bethesda.Execution.Groups;

namespace Synthesis.Bethesda.Execution.Running.Runner;

public interface IRunSomePatchers
{
    Task<FilePath?> Run(
        IGroupRun groupRun,
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
        IGroupRun groupRun,
        PatcherPrepBundle[] patchers,
        CancellationToken cancellation,
        FilePath? sourcePath,
        RunParameters runParameters)
    {
        for (int i = 0; i < patchers.Length; i++)
        {
            var patcher = patchers[i];

            var nextPath = await RunAPatcher.Run(
                groupRun,
                patcher,
                cancellation,
                sourcePath,
                runParameters).ConfigureAwait(false);

            if (nextPath != null)
            {
                sourcePath = nextPath;
            }
        }

        return sourcePath;
    }
}