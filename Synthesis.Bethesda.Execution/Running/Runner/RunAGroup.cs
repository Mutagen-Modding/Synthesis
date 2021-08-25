using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Noggog;
using Synthesis.Bethesda.Execution.Groups;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.Execution.Running.Runner
{
    public interface IRunAGroup
    {
        Task<bool> Run(
            IGroupRun groupRun,
            CancellationToken cancellation,
            DirectoryPath outputDir,
            FilePath? sourcePath = null,
            PersistenceMode persistenceMode = PersistenceMode.None,
            string? persistencePath = null);
    }

    public class RunAGroup : IRunAGroup
    {
        public IMoveFinalResults MoveFinalResults { get; }
        public IRunAPatcher RunPatcher { get; }
        public IGroupRunPreparer GroupRunPreparer { get; }

        public RunAGroup(
            IGroupRunPreparer groupRunPreparer,
            IRunAPatcher runPatcher,
            IMoveFinalResults moveFinalResults)
        {
            GroupRunPreparer = groupRunPreparer;
            MoveFinalResults = moveFinalResults;
            RunPatcher = runPatcher;
        }
        
        public async Task<bool> Run(
            IGroupRun groupRun,
            CancellationToken cancellation,
            DirectoryPath outputDir,
            FilePath? sourcePath = null,
            PersistenceMode persistenceMode = PersistenceMode.None,
            string? persistencePath = null)
        {
            await GroupRunPreparer.Prepare(groupRun.ModKey, persistenceMode, persistencePath);
            
            for (int i = 0; i < groupRun.Patchers.Length; i++)
            {
                var patcher = groupRun.Patchers[i];

                var nextPath = await RunPatcher.Run(
                    outputKey: groupRun.ModKey,
                    patcher,
                    cancellation,
                    sourcePath,
                    persistencePath);

                if (nextPath == null) return false;
                
                sourcePath = nextPath;
            }

            if (sourcePath == null) return false;

            cancellation.ThrowIfCancellationRequested();
            MoveFinalResults.Move(sourcePath.Value, Path.Combine(outputDir.Path, groupRun.ModKey.FileName));
            return true;
        }
    }
}