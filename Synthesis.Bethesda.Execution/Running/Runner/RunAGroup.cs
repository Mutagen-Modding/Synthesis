using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Noggog;
using Serilog;
using Synthesis.Bethesda.Execution.Groups;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.Execution.Running.Runner;

public interface IRunAGroup
{
    Task<bool> Run(
        IGroupRun groupRun,
        CancellationToken cancellation,
        DirectoryPath outputDir,
        RunParameters runParameters,
        FilePath? sourcePath = null);
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
        RunParameters runParameters,
        FilePath? sourcePath = null)
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
            
        await GroupRunPreparer.Prepare(
            groupRun, 
            groupRun.BlacklistedMods,
            runParameters.PersistenceMode, 
            runParameters.PersistencePath).ConfigureAwait(false);

        var finalPath = await RunSomePatchers.Run(
            groupRun,
            groupRun.Patchers,
            cancellation,
            sourcePath,
            runParameters).ConfigureAwait(false);

        if (finalPath == null) return false;

        cancellation.ThrowIfCancellationRequested();
        MoveFinalResults.Move(finalPath.Value, outputDir);
        return true;
    }
}