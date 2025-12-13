using System.Reactive.Disposables;
using Synthesis.Bethesda.Commands;

namespace Synthesis.Bethesda.Execution.Patchers.Running.Solution;

public interface ISolutionPatcherPrepAndRun : IPatcherPrepAndRun
{
}

public class SolutionPatcherPrepAndRun : ISolutionPatcherPrepAndRun
{
    private readonly CompositeDisposable _disposable = new();

    public Guid Key => RunExecution.Key;
    public int Index => RunExecution.Index;
    public string Name => RunExecution.Name;

    public ISolutionPatcherPrep PrepService { get; }
    public ISolutionPatcherRunExecution RunExecution { get; }

    public SolutionPatcherPrepAndRun(
        ISolutionPatcherPrep prep,
        ISolutionPatcherRunExecution runExecution)
    {
        PrepService = prep;
        RunExecution = runExecution;
    }

    public async Task Prep(CancellationToken cancel)
    {
        await PrepService.Prep(cancel).ConfigureAwait(false);
    }

    public async Task Run(RunSynthesisPatcher settings, CancellationToken cancel)
    {
        await RunExecution.Run(settings, cancel).ConfigureAwait(false);
    }

    public void Dispose()
    {
        _disposable.Dispose();
    }

    public void Add(IDisposable disposable)
    {
        _disposable.Add(disposable);
    }

    public override string ToString() => Name;
}
