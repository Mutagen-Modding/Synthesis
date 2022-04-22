using System.IO.Abstractions;
using Noggog;
using Serilog;
using Synthesis.Bethesda.Execution.Groups;
using Synthesis.Bethesda.Execution.Reporters;

namespace Synthesis.Bethesda.Execution.Running.Runner;

public interface IRunAPatcher
{
    Task<FilePath?> Run(
        IGroupRun groupRun,
        PatcherPrepBundle prepBundle,
        CancellationToken cancellation,
        FilePath? sourcePath,
        RunParameters runParameters);
}

public class RunAPatcher : IRunAPatcher
{
    private readonly ILogger _logger;
    private readonly IRunReporter _reporter;
    private readonly IFileSystem _fs;
    public IFinalizePatcherRun FinalizePatcherRun { get; }
    public IRunArgsConstructor GetRunArgs { get; }

    public RunAPatcher(
        ILogger logger,
        IRunReporter reporter,
        IFileSystem fs,
        IFinalizePatcherRun finalizePatcherRun,
        IRunArgsConstructor getRunArgs)
    {
        _logger = logger;
        _reporter = reporter;
        _fs = fs;
        FinalizePatcherRun = finalizePatcherRun;
        GetRunArgs = getRunArgs;
    }

    public async Task<FilePath?> Run(
        IGroupRun groupRun,
        PatcherPrepBundle prepBundle,
        CancellationToken cancellation,
        FilePath? sourcePath,
        RunParameters runParameters)
    {
        try
        {
            // Finish waiting for prep, if it didn't finish
            var prepException = await prepBundle.Prep.ConfigureAwait(false);
            if (prepException != null) return null;
                
            var args = GetRunArgs.GetArgs(
                groupRun,
                prepBundle.Run,
                sourcePath,
                runParameters);

            _fs.Directory.CreateDirectory(args.OutputPath.Directory!);
                
            _logger.Information("================= Starting Patcher {Patcher} Run =================", prepBundle.Run.Name);

            _reporter.ReportStartingRun(prepBundle.Run.Key, prepBundle.Run.Name);
            await prepBundle.Run.Run(args,
                cancel: cancellation).ConfigureAwait(false);
                
            if (cancellation.IsCancellationRequested) return null;

            return FinalizePatcherRun.Finalize(prepBundle.Run, args.OutputPath);
        }
        catch (TaskCanceledException)
        {
            return null;
        }
        catch (Exception ex)
        {
            _reporter.ReportRunProblem(prepBundle.Run.Key, prepBundle.Run.Name, ex);
            throw;
        }
    }
}