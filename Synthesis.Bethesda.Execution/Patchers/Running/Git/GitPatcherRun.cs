using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using Noggog;
using Serilog;
using Synthesis.Bethesda.Commands;
using Synthesis.Bethesda.Execution.GitRepository;
using Synthesis.Bethesda.Execution.Patchers.Common;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Patchers.Running.Solution;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.Execution.Patchers.Running.Git;

public interface IGitPatcherRun : IPatcherRun
{
}

[ExcludeFromCodeCoverage]
public class GitPatcherRun : IGitPatcherRun
{
    private readonly CompositeDisposable _disposable = new();

    public Guid Key { get; }
    public int Index { get; }
    public string Name { get; }
        
    private readonly GithubPatcherSettings _settings;
    private readonly ILogger _logger;
    private readonly IRunnerRepoDirectoryProvider _runnerRepoDirectoryProvider;
    private readonly ICheckOrCloneRepo _checkOrClone;
    public ISolutionPatcherRun SolutionPatcherRun { get; }

    public GitPatcherRun(
        GithubPatcherSettings settings,
        ILogger logger,
        IPatcherIdProvider idProvider,
        IIndexDisseminator indexDisseminator,
        IRunnerRepoDirectoryProvider runnerRepoDirectoryProvider,
        ISolutionPatcherRun solutionPatcherRun,
        ICheckOrCloneRepo checkOrClone)
    {
        Key = idProvider.InternalId;
        SolutionPatcherRun = solutionPatcherRun;
        _settings = settings;
        _logger = logger;
        _runnerRepoDirectoryProvider = runnerRepoDirectoryProvider;
        _checkOrClone = checkOrClone;
        Index = indexDisseminator.GetNext();
        Name = $"{settings.Nickname.Decorate(x => $"{x} => ")}{settings.RemoteRepoPath} => {Path.GetFileNameWithoutExtension(settings.SelectedProjectSubpath)}";
    }
        
    public void Dispose()
    {
        _disposable.Dispose();
    }

    public void Add(IDisposable disposable)
    {
        _disposable.Add(disposable);
    }

    public async Task Prep(CancellationToken cancel)
    {
        _logger.Information("Cloning repository");
        var cloneResult = _checkOrClone.Check(
            GetResponse<string>.Succeed(_settings.RemoteRepoPath),
            _runnerRepoDirectoryProvider.Path,
            cancel);
        if (cloneResult.Failed)
        {
            throw new SynthesisBuildFailure(cloneResult.Reason);
        }

        await SolutionPatcherRun.Prep(cancel).ConfigureAwait(false);
    }

    public async Task Run(RunSynthesisPatcher settings, CancellationToken cancel)
    {
        await SolutionPatcherRun.Run(settings, cancel).ConfigureAwait(false);
    }
}