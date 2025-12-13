using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using Noggog;
using Synthesis.Bethesda.Commands;
using Synthesis.Bethesda.Execution.Patchers.Common;
using Synthesis.Bethesda.Execution.Patchers.Running.Solution;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.Execution.Patchers.Running.Git;

public interface IGitPatcherRun : IPatcherPrepAndRun
{
}

[ExcludeFromCodeCoverage]
public class GitPatcherRun : IGitPatcherRun
{
    private readonly CompositeDisposable _disposable = new();

    public Guid Key => SolutionPatcherRunExecution.Key;
    public int Index => SolutionPatcherRunExecution.Index;
    public string Name => SolutionPatcherRunExecution.Name;

    public IGitPatcherPrep GitPatcherPrep { get; }
    public ISolutionPatcherPrep SolutionPatcherPrep { get; }
    public ISolutionPatcherRunExecution SolutionPatcherRunExecution { get; }

    public GitPatcherRun(
        IGitPatcherPrep gitPatcherPrep,
        ISolutionPatcherPrep solutionPatcherPrep,
        ISolutionPatcherRunExecution solutionPatcherRunExecution)
    {
        GitPatcherPrep = gitPatcherPrep;
        SolutionPatcherPrep = solutionPatcherPrep;
        SolutionPatcherRunExecution = solutionPatcherRunExecution;
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
        await SolutionPatcherPrep.Prep(cancel).ConfigureAwait(false);
    }

    public async Task Run(RunSynthesisPatcher settings, CancellationToken cancel)
    {
        await SolutionPatcherRunExecution.Run(settings, cancel).ConfigureAwait(false);
    }
}