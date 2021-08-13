using System;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using Mutagen.Bethesda.Plugins;
using Noggog;
using Synthesis.Bethesda.Execution.Patchers.Running;
using Synthesis.Bethesda.Execution.Reporters;

namespace Synthesis.Bethesda.Execution.Running.Runner
{
    public interface IRunAPatcher
    {
        Task<FilePath?> Run(
            ModKey outputKey,
            IPatcherRun patcher,
            Task<Exception?> patcherPrep,
            CancellationToken cancellation,
            FilePath? sourcePath,
            string? persistencePath);
    }

    public class RunAPatcher : IRunAPatcher
    {
        private readonly IRunReporter _reporter;
        public IFinalizePatcherRun FinalizePatcherRun { get; }
        public IRunArgsConstructor GetRunArgs { get; }

        public RunAPatcher(
            IRunReporter reporter,
            IFinalizePatcherRun finalizePatcherRun,
            IRunArgsConstructor getRunArgs)
        {
            _reporter = reporter;
            FinalizePatcherRun = finalizePatcherRun;
            GetRunArgs = getRunArgs;
        }

        public async Task<FilePath?> Run(
            ModKey outputKey,
            IPatcherRun patcher,
            Task<Exception?> patcherPrep,
            CancellationToken cancellation,
            FilePath? sourcePath,
            string? persistencePath)
        {
            try
            {
                // Finish waiting for prep, if it didn't finish
                var prepException = await patcherPrep;
                if (prepException != null) return null;
                
                var args = GetRunArgs.GetArgs(
                    patcher,
                    outputKey,
                    sourcePath,
                    persistencePath);
                
                _reporter.ReportStartingRun(patcher.Key, patcher.Name);
                await patcher.Run(args,
                    cancel: cancellation);
                
                if (cancellation.IsCancellationRequested) return null;

                return FinalizePatcherRun.Finalize(patcher, args.OutputPath);
            }
            catch (TaskCanceledException)
            {
                return null;
            }
            catch (Exception ex)
            {
                _reporter.ReportRunProblem(patcher.Key, patcher.Name, ex);
                return null;
            }
        }
    }
}