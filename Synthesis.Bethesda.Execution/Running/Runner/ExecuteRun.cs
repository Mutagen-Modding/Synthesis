using Noggog;
using Synthesis.Bethesda.Execution.Groups;

namespace Synthesis.Bethesda.Execution.Running.Runner;

public interface IExecuteRun
{
    Task Run(
        IGroupRun[] groups,
        CancellationToken cancel,
        DirectoryPath outputDir,
        RunParameters runParameters,
        FilePath? sourcePath = null);
}

public class ExecuteRun : IExecuteRun
{
    private readonly IPrintRunStart _print;
    public IResetWorkingDirectory ResetWorkingDirectory { get; }
    public IRunAllGroups RunAllGroups { get; }
    public IEnsureSourcePathExists EnsureSourcePathExists { get; }

    public ExecuteRun(
        IResetWorkingDirectory resetWorkingDirectory,
        IRunAllGroups runAllGroups,
        IPrintRunStart print,
        IEnsureSourcePathExists ensureSourcePathExists)
    {
        _print = print;
        ResetWorkingDirectory = resetWorkingDirectory;
        RunAllGroups = runAllGroups;
        EnsureSourcePathExists = ensureSourcePathExists;
    }

    public async Task Run(
        IGroupRun[] groups,
        CancellationToken cancellation,
        DirectoryPath outputDir,
        RunParameters runParameters,
        FilePath? sourcePath = null)
    {
        _print.Print(groups);
            
        cancellation.ThrowIfCancellationRequested();
        ResetWorkingDirectory.Reset();

        if (groups.Length == 0) return;

        cancellation.ThrowIfCancellationRequested();
        EnsureSourcePathExists.Ensure(sourcePath);

        cancellation.ThrowIfCancellationRequested();

        await RunAllGroups.Run(
            groups,
            cancellation,
            outputDir,
            runParameters,
            sourcePath).ConfigureAwait(false);
    }
}