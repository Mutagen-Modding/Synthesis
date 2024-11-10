using Noggog;
using Serilog;
using Synthesis.Bethesda.Execution.Groups;
using Synthesis.Bethesda.Execution.Profile.Services;

namespace Synthesis.Bethesda.Execution.Running.Runner;

public interface IExecuteRun
{
    Task Run(
        IGroupRun[] groups,
        CancellationToken cancel,
        DirectoryPath outputDir,
        RunParameters runParameters);
}

public class ExecuteRun : IExecuteRun
{
    private readonly ILogger _logger;
    private readonly IPrintRunStart _print;
    private readonly ILoadOrderPrinter _loadOrderPrinter;
    private readonly IProfileLoadOrderProvider _profileLoadOrderProvider;
    public IResetWorkingDirectory ResetWorkingDirectory { get; }
    public IRunAllGroups RunAllGroups { get; }
    public IEnsureSourcePathExists EnsureSourcePathExists { get; }

    public ExecuteRun(
        IResetWorkingDirectory resetWorkingDirectory,
        IRunAllGroups runAllGroups,
        IPrintRunStart print,
        ILoadOrderPrinter loadOrderPrinter,
        IProfileLoadOrderProvider profileLoadOrderProvider,
        IEnsureSourcePathExists ensureSourcePathExists,
        ILogger logger)
    {
        _print = print;
        _loadOrderPrinter = loadOrderPrinter;
        _profileLoadOrderProvider = profileLoadOrderProvider;
        ResetWorkingDirectory = resetWorkingDirectory;
        RunAllGroups = runAllGroups;
        EnsureSourcePathExists = ensureSourcePathExists;
        _logger = logger;
    }

    public async Task Run(
        IGroupRun[] groups,
        CancellationToken cancellation,
        DirectoryPath outputDir,
        RunParameters runParameters)
    {
        _print.Print(groups);
        
        _logger.Information("Raw Load Order:");
        _loadOrderPrinter.Print(_profileLoadOrderProvider.Get());
            
        cancellation.ThrowIfCancellationRequested();
        ResetWorkingDirectory.Reset();

        if (groups.Length == 0) return;

        cancellation.ThrowIfCancellationRequested();

        await RunAllGroups.Run(
            groups,
            cancellation,
            outputDir,
            runParameters).ConfigureAwait(false);
    }
}