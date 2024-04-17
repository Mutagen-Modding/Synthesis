using Noggog;
using Serilog;
using Synthesis.Bethesda.Execution.Groups;

namespace Synthesis.Bethesda.Execution.Running.Runner;

public interface IRunAGroup
{
    Task<bool> Run(
        IGroupRun groupRun,
        CancellationToken cancellation,
        DirectoryPath outputDir,
        RunParameters runParameters);
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
        RunParameters runParameters)
    {
        _logger.Information("================= Starting Group Run: {Group} =================", groupRun.ModKey.Name);
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
            runParameters).ConfigureAwait(false);

        var finalPath = await RunSomePatchers.Run(
            groupRun,
            groupRun.Patchers,
            cancellation,
            null,
            runParameters).ConfigureAwait(false);

        if (finalPath == null) return false;

        cancellation.ThrowIfCancellationRequested();
        MoveFinalResults.Move(finalPath.Value, outputDir);
        return true;
    }
}