using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Noggog;
using Serilog;
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
        private readonly ILogger _logger;
        public IMoveFinalResults MoveFinalResults { get; }
        public IGroupRunPreparer GroupRunPreparer { get; }
        public IRunSomePatchers RunSomePatchers { get; }

        public RunAGroup(
            ILogger logger,
            IGroupRunPreparer groupRunPreparer,
            IRunSomePatchers runSomePatchers,
            IMoveFinalResults moveFinalResults)
        {
            _logger = logger;
            GroupRunPreparer = groupRunPreparer;
            RunSomePatchers = runSomePatchers;
            MoveFinalResults = moveFinalResults;
        }
        
        public async Task<bool> Run(
            IGroupRun groupRun,
            CancellationToken cancellation,
            DirectoryPath outputDir,
            FilePath? sourcePath = null,
            PersistenceMode persistenceMode = PersistenceMode.None,
            string? persistencePath = null)
        {
            _logger.Information("================= Starting Group {Group} Run =================", groupRun.ModKey.Name);
            if (groupRun.BlacklistedMods.Count > 0)
            {
                _logger.Information("Blacklisting mods:");
                foreach (var mod in groupRun.BlacklistedMods.OrderBy(x => x.Name))
                {
                    _logger.Information("   {Mod}", mod);
                }
            }
            
            await GroupRunPreparer.Prepare(groupRun.ModKey, groupRun.BlacklistedMods, persistenceMode, persistencePath).ConfigureAwait(false);

            var finalPath = await RunSomePatchers.Run(
                groupRun.ModKey,
                groupRun.Patchers,
                cancellation,
                sourcePath,
                persistenceMode == PersistenceMode.None ? null : persistencePath).ConfigureAwait(false);

            if (finalPath == null) return false;

            cancellation.ThrowIfCancellationRequested();
            MoveFinalResults.Move(finalPath.Value, outputDir);
            return true;
        }
    }
}