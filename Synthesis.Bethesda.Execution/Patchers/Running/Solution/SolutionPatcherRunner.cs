using Serilog;
using Synthesis.Bethesda.Commands;
using Synthesis.Bethesda.Execution.PatcherCommands;
using Synthesis.Bethesda.Execution.Patchers.Solution;
using Synthesis.Bethesda.Execution.Utility;

namespace Synthesis.Bethesda.Execution.Patchers.Running.Solution;

public interface ISolutionPatcherRunner
{
    Task Run(RunSynthesisPatcher settings, CancellationToken cancel);
}

public class SolutionPatcherRunner : ISolutionPatcherRunner
{
    public IPathToProjProvider PathToProjProvider { get; }
    public IConstructSolutionPatcherRunArgs ConstructArgs { get; }
    public ISynthesisSubProcessRunner ProcessRunner { get; }
    public IFormatCommandLine Formatter { get; }
    public IProjectRunProcessStartInfoProvider ProcessRunStartInfoProvider { get; }
    private readonly ILogger _logger;

    public SolutionPatcherRunner(
        IProjectRunProcessStartInfoProvider processRunStartInfoProvider,
        IPathToProjProvider pathToProjProvider,
        IConstructSolutionPatcherRunArgs constructArgs,
        ISynthesisSubProcessRunner processRunner,
        IFormatCommandLine formatter,
        ILogger logger)
    {
        PathToProjProvider = pathToProjProvider;
        ConstructArgs = constructArgs;
        ProcessRunner = processRunner;
        Formatter = formatter;
        ProcessRunStartInfoProvider = processRunStartInfoProvider;
        _logger = logger;
    }
        
    public async Task Run(RunSynthesisPatcher settings, CancellationToken cancel)
    {
        _logger.Information("Running");
        var args = ConstructArgs.Construct(settings);
        var result = await ProcessRunner.Run(
            ProcessRunStartInfoProvider.GetStart(
                PathToProjProvider.Path,
                Formatter.Format(args),
                build: false),
            cancel: cancel).ConfigureAwait(false);
            
        if (result != 0)
        {
            throw new CliUnsuccessfulRunException(result, "Error running solution patcher");
        }
    }
}