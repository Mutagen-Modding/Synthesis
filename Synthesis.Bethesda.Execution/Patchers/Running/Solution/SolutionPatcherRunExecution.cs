using System.Reactive.Disposables;
using Synthesis.Bethesda.Commands;
using Synthesis.Bethesda.Execution.Patchers.Common;
using Synthesis.Bethesda.Execution.Patchers.Solution;

namespace Synthesis.Bethesda.Execution.Patchers.Running.Solution;

public interface ISolutionPatcherRunExecution : IPatcherRunExecution
{
}

public class SolutionPatcherRunExecution : ISolutionPatcherRunExecution
{
    private readonly CompositeDisposable _disposable = new();

    public Guid Key { get; }
    public int Index { get; }
    public IPatcherNameProvider NameProvider { get; }
    public IPathToProjProvider PathToProjProvider { get; }
    public ISolutionPatcherRunner SolutionPatcherRunner { get; }
    public IPrintShaIfApplicable PrintShaIfApplicable { get; }
    public string Name => NameProvider.Name;

    public SolutionPatcherRunExecution(
        IPatcherNameProvider nameProvider,
        IPathToProjProvider pathToProjProvider,
        ISolutionPatcherRunner solutionPatcherRunner,
        IPatcherIdProvider idProvider,
        IIndexDisseminator indexDisseminator,
        IPrintShaIfApplicable printShaIfApplicable)
    {
        Key = idProvider.InternalId;
        NameProvider = nameProvider;
        PathToProjProvider = pathToProjProvider;
        SolutionPatcherRunner = solutionPatcherRunner;
        PrintShaIfApplicable = printShaIfApplicable;
        Index = indexDisseminator.GetNext();
    }

    public async Task Run(RunSynthesisPatcher settings, CancellationToken cancel)
    {
        PrintShaIfApplicable.Print();

        await SolutionPatcherRunner.Run(settings, cancel).ConfigureAwait(false);
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
