using Noggog;
using Noggog.GitRepository;
using Serilog;
using Synthesis.Bethesda.Execution.Exceptions;
using Synthesis.Bethesda.Execution.GitRepository;
using Synthesis.Bethesda.Execution.Patchers.Git.Services;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.Execution.Patchers.Running.Git;

public interface IGitPatcherPrep
{
    Task Prep(CancellationToken cancel);
}

public class GitPatcherPrep : IGitPatcherPrep
{
    private readonly GithubPatcherSettings _settings;
    private readonly ILogger _logger;
    private readonly IRunnerRepoDirectoryProvider _runnerRepoDirectoryProvider;
    private readonly ICheckIfRepositoryDesirable _checkIfRepositoryDesirable;
    private readonly ICheckOrCloneRepo _checkOrClone;

    public GitPatcherPrep(
        GithubPatcherSettings settings,
        ILogger logger,
        IRunnerRepoDirectoryProvider runnerRepoDirectoryProvider,
        ICheckIfRepositoryDesirable checkIfRepositoryDesirable,
        ICheckOrCloneRepo checkOrClone)
    {
        _settings = settings;
        _logger = logger;
        _runnerRepoDirectoryProvider = runnerRepoDirectoryProvider;
        _checkIfRepositoryDesirable = checkIfRepositoryDesirable;
        _checkOrClone = checkOrClone;
    }

    public Task Prep(CancellationToken cancel)
    {
        _logger.Information("Cloning repository");
        var cloneResult = _checkOrClone.Check(
            GetResponse<string>.Succeed(_settings.RemoteRepoPath),
            _runnerRepoDirectoryProvider.Path,
            isDesirable: _checkIfRepositoryDesirable.IsDesirable,
            cancel: cancel);
        if (cloneResult.Failed)
        {
            throw new SynthesisBuildFailure(cloneResult.Reason);
        }

        return Task.CompletedTask;
    }
}
