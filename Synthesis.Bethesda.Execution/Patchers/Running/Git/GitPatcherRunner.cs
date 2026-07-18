using Serilog;
using Synthesis.Bethesda.Commands;
using Synthesis.Bethesda.Execution.Exceptions;
using Synthesis.Bethesda.Execution.PatcherCommands;
using Synthesis.Bethesda.Execution.Patchers.Running.Solution;
using Synthesis.Bethesda.Execution.Utility;

namespace Synthesis.Bethesda.Execution.Patchers.Running.Git;

public interface IGitPatcherRunner
{
    Task Run(RunSynthesisPatcher settings, GitCompilationMeta meta, PatcherRunCapture capture, CancellationToken cancel);
}

public class GitPatcherRunner : IGitPatcherRunner
{
    private readonly IConstructSolutionPatcherRunArgs _constructArgs;
    private readonly ISynthesisSubProcessRunner _processRunner;
    private readonly IRunProcessStartInfoProvider _runProcessStartInfoProvider;
    private readonly IMo2RetryCoordinator _mo2RetryCoordinator;
    private readonly ILogger _logger;

    public GitPatcherRunner(
        IConstructSolutionPatcherRunArgs constructArgs,
        ISynthesisSubProcessRunner processRunner,
        IRunProcessStartInfoProvider runProcessStartInfoProvider,
        IMo2RetryCoordinator mo2RetryCoordinator,
        ILogger logger)
    {
        _constructArgs = constructArgs;
        _processRunner = processRunner;
        _runProcessStartInfoProvider = runProcessStartInfoProvider;
        _mo2RetryCoordinator = mo2RetryCoordinator;
        _logger = logger;
    }

    public async Task Run(RunSynthesisPatcher settings, GitCompilationMeta meta, PatcherRunCapture capture, CancellationToken cancel)
    {
        if (string.IsNullOrWhiteSpace(meta.ExecutablePath))
        {
            throw new InvalidOperationException("Git patcher compilation meta is missing ExecutablePath");
        }

        if (!File.Exists(meta.ExecutablePath))
        {
            throw new FileNotFoundException($"Compiled git patcher executable not found at: {meta.ExecutablePath}");
        }

        _logger.Information("Running compiled git patcher executable: {Path}", meta.ExecutablePath);

        var args = _constructArgs.Construct(settings);

        var exitCode = await _mo2RetryCoordinator.OnTransientSpawnFailure(
            () => _processRunner.RunWithCapture(
                _runProcessStartInfoProvider.GetStart(meta.ExecutablePath, args),
                capture,
                cancel: cancel),
            cancel).ConfigureAwait(false);

        if (exitCode != 0)
        {
            throw new CliUnsuccessfulRunException(
                exitCode,
                "Error running git patcher executable");
        }
    }
}