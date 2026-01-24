using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using Synthesis.Bethesda.Commands;
using Synthesis.Bethesda.Execution.Utility;

namespace Synthesis.Bethesda.Execution.Patchers.Running.Git;

public interface IGitPatcherRun : IPatcherPrepAndRun
{
}

[ExcludeFromCodeCoverage]
public class GitPatcherRun : IGitPatcherRun
{
    private readonly CompositeDisposable _disposable = new();

    public Guid Key => GitPatcherRunExecution.Key;
    public int Index => GitPatcherRunExecution.Index;
    public string Name => GitPatcherRunExecution.Name;

    public IGitPatcherPrep GitPatcherPrep { get; }
    public IGitPatcherRunExecution GitPatcherRunExecution { get; }

    public GitPatcherRun(
        IGitPatcherPrep gitPatcherPrep,
        IGitPatcherRunExecution gitPatcherRunExecution)
    {
        GitPatcherPrep = gitPatcherPrep;
        GitPatcherRunExecution = gitPatcherRunExecution;
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
        await GitPatcherPrep.Prep(cancel).ConfigureAwait(false);
    }

    public async Task Run(RunSynthesisPatcher settings, PatcherRunCapture capture, CancellationToken cancel)
    {
        await GitPatcherRunExecution.Run(settings, capture, cancel).ConfigureAwait(false);
    }
}