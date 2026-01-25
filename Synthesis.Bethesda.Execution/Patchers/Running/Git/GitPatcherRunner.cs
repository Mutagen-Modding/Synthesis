using System.Diagnostics;
using Serilog;
using Synthesis.Bethesda.Commands;
using Synthesis.Bethesda.Execution.Exceptions;
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
    private readonly IFormatCommandLine _formatter;
    private readonly ILogger _logger;

    public GitPatcherRunner(
        IConstructSolutionPatcherRunArgs constructArgs,
        ISynthesisSubProcessRunner processRunner,
        IFormatCommandLine formatter,
        ILogger logger)
    {
        _constructArgs = constructArgs;
        _processRunner = processRunner;
        _formatter = formatter;
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

        var exitCode = await _processRunner.RunWithCapture(
            new ProcessStartInfo("dotnet", $"\"{meta.ExecutablePath}\" {_formatter.Format(args)}"),
            capture,
            cancel: cancel).ConfigureAwait(false);

        if (exitCode != 0)
        {
            throw new CliUnsuccessfulRunException(
                exitCode,
                "Error running git patcher executable");
        }
    }
}