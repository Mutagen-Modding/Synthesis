using System.Threading;
using System.Threading.Tasks;
using Mutagen.Bethesda.Plugins;
using Noggog;
using Synthesis.Bethesda.Execution.Patchers.Running;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.Execution.Running.Runner
{
    public interface IExecuteRun
    {
        Task<bool> Run(
            ModPath outputPath,
            (int Key, IPatcherRun Run)[] patchers,
            CancellationToken cancel,
            FilePath? sourcePath = null,
            PersistenceMode persistenceMode = PersistenceMode.None,
            string? persistencePath = null);
    }

    public class ExecuteRun : IExecuteRun
    {
        public IOverallRunPreparer OverallRunPreparer { get; }
        public IResetWorkingDirectory ResetWorkingDirectory { get; }
        public IPrepPatchersForRun PrepPatchersForRun { get; }
        public IRunAllPatchers RunAllPatchers { get; }
        public IMoveFinalResults MoveFinalResults { get; }
        public IEnsureSourcePathExists EnsureSourcePathExists { get; }

        public ExecuteRun(
            IOverallRunPreparer overallRunPreparer,
            IResetWorkingDirectory resetWorkingDirectory,
            IPrepPatchersForRun prepPatchersForRun,
            IRunAllPatchers runAllPatchers,
            IMoveFinalResults moveFinalResults,
            IEnsureSourcePathExists ensureSourcePathExists)
        {
            OverallRunPreparer = overallRunPreparer;
            ResetWorkingDirectory = resetWorkingDirectory;
            PrepPatchersForRun = prepPatchersForRun;
            RunAllPatchers = runAllPatchers;
            MoveFinalResults = moveFinalResults;
            EnsureSourcePathExists = ensureSourcePathExists;
        }

        public async Task<bool> Run(
            ModPath outputPath,
            (int Key, IPatcherRun Run)[] patchers,
            CancellationToken cancellation,
            FilePath? sourcePath = null,
            PersistenceMode persistenceMode = PersistenceMode.None,
            string? persistencePath = null)
        {
            cancellation.ThrowIfCancellationRequested();
            ResetWorkingDirectory.Reset();

            if (patchers.Length == 0) return false;

            cancellation.ThrowIfCancellationRequested();
            EnsureSourcePathExists.Ensure(sourcePath);

            var overallPrep = OverallRunPreparer.Prepare(outputPath, persistenceMode, persistencePath);

            var patcherPreps = PrepPatchersForRun.PrepPatchers(patchers, cancellation);

            await overallPrep;
            cancellation.ThrowIfCancellationRequested();

            var finalPatch = await RunAllPatchers.Run(
                outputPath.ModKey,
                patchers,
                patcherPreps,
                cancellation,
                sourcePath,
                persistencePath);
            if (finalPatch == null) return false;

            cancellation.ThrowIfCancellationRequested();
            MoveFinalResults.Move(finalPatch.Value, outputPath);
            return true;
        }
    }
}