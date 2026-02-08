using Mutagen.Bethesda.Plugins;
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
    private readonly PostRunProcessor _postRunProcessor;
    private readonly ILoadOrderMaintainer _loadOrderMaintainer;
    public IMoveFinalResults MoveFinalResults { get; }
    public IGroupRunPreparer GroupRunPreparer { get; }
    public IRunSomePatchers RunSomePatchers { get; }

    public RunAGroup(
        ILogger logger,
        IGroupRunPreparer groupRunPreparer,
        IRunSomePatchers runSomePatchers,
        IMoveFinalResults moveFinalResults,
        PostRunProcessor postRunProcessor,
        ILoadOrderMaintainer loadOrderMaintainer)
    {
        _logger = logger;
        _postRunProcessor = postRunProcessor;
        _loadOrderMaintainer = loadOrderMaintainer;
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
            
        var sourcePath = await GroupRunPreparer.Prepare(
            groupRun, 
            groupRun.BlacklistedMods,
            runParameters).ConfigureAwait(false);

        var patcherRunOutputPath = await RunSomePatchers.Run(
            groupRun,
            groupRun.Patchers,
            cancellation,
            sourcePath,
            runParameters).ConfigureAwait(false);

        if (patcherRunOutputPath == null) return false;
        
        cancellation.ThrowIfCancellationRequested();
        var postRunPath = await _postRunProcessor.Run(
            groupRun,
            new ModPath(groupRun.ModKey, patcherRunOutputPath.Value),
            groupRun.BlacklistedMods,
            runParameters);
        
        cancellation.ThrowIfCancellationRequested();
        var movedModKeys = MoveFinalResults.Move(postRunPath, outputDir);

        if (runParameters.SplitIfMaxMastersExceeded && runParameters.UpdateLoadOrderAfterRun)
        {
            _loadOrderMaintainer.UpdateLoadOrder(groupRun.ModKey, movedModKeys);
        }

        return true;
    }
}