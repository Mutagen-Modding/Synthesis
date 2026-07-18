using System.IO.Abstractions;
using System.Runtime.ExceptionServices;
using Noggog;
using Serilog;
using Synthesis.Bethesda.Execution.Exceptions;
using Synthesis.Bethesda.Execution.Groups;
using Synthesis.Bethesda.Execution.Reporters;
using Synthesis.Bethesda.Execution.Utility;

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
    private readonly IFormatCommandLine _formatCommandLine;
    private readonly ILoadOrderForRunProvider _loadOrderForRunProvider;
    public IFinalizePatcherRun FinalizePatcherRun { get; }
    public IRunArgsConstructor GetRunArgs { get; }

    public RunAPatcher(
        ILogger logger,
        IRunReporter reporter,
        IFileSystem fs,
        IFormatCommandLine formatCommandLine,
        IFinalizePatcherRun finalizePatcherRun,
        IRunArgsConstructor getRunArgs,
        ILoadOrderForRunProvider loadOrderForRunProvider)
    {
        _logger = logger;
        _reporter = reporter;
        _fs = fs;
        _formatCommandLine = formatCommandLine;
        FinalizePatcherRun = finalizePatcherRun;
        GetRunArgs = getRunArgs;
        _loadOrderForRunProvider = loadOrderForRunProvider;
    }

    public async Task<FilePath?> Run(
        IGroupRun groupRun,
        PatcherPrepBundle prepBundle,
        CancellationToken cancellation,
        FilePath? sourcePath,
        RunParameters runParameters)
    {
        var capture = new PatcherRunCapture();

        try
        {
            // Finish waiting for prep, if it didn't finish
            var prepException = await prepBundle.Prep.ConfigureAwait(false);
            if (cancellation.IsCancellationRequested) return null;
            if (prepException != null)
            {
                ExceptionDispatchInfo.Capture(prepException).Throw();
                throw prepException;
            }

            var args = GetRunArgs.GetArgs(
                groupRun,
                prepBundle.Run,
                sourcePath,
                runParameters);

            _fs.Directory.CreateDirectory(args.OutputPath.Directory!);

            _logger.Information("================= Starting Patcher {Patcher} Run =================", prepBundle.Run.Name);
            _logger.Information(_formatCommandLine.Format(args));

            _reporter.ReportStartingRun(prepBundle.Run.Key, prepBundle.Run.Name);
            await prepBundle.Run.Run(args, capture,
                cancel: cancellation).ConfigureAwait(false);

            if (cancellation.IsCancellationRequested) return null;

            return FinalizePatcherRun.Finalize(prepBundle.Run, args.OutputPath);
        }
        catch (TaskCanceledException)
        {
            return null;
        }
        catch (CliUnsuccessfulRunException)
        {
            // Get the load order for error classification
            var loadOrder = _loadOrderForRunProvider.Get(groupRun.ModKey, groupRun.BlacklistedMods);

            _reporter.ReportRunProblem(
                prepBundle.Run.Key,
                prepBundle.Run.Name,
                null,
                capture.Output,
                capture.Errors,
                loadOrder);
            throw;
        }
        catch (Exception ex)
        {
            // Get the load order for error classification
            var loadOrder = _loadOrderForRunProvider.Get(groupRun.ModKey, groupRun.BlacklistedMods);

            _reporter.ReportRunProblem(
                prepBundle.Run.Key,
                prepBundle.Run.Name,
                ex,
                capture.Output,
                capture.Errors,
                loadOrder);
            throw;
        }
    }
}