using System.Reactive.Disposables;
using Synthesis.Bethesda.Commands;
using Synthesis.Bethesda.Execution.Patchers.Common;
using Synthesis.Bethesda.Execution.Patchers.Solution;

namespace Synthesis.Bethesda.Execution.Patchers.Running.Solution;

public interface ISolutionPatcherRun : IPatcherRun
{
}

public class SolutionPatcherRun : ISolutionPatcherRun
{
    private readonly CompositeDisposable _disposable = new();
        
    public Guid Key { get; }
    public int Index { get; }
    public IPatcherNameProvider NameProvider { get; }
    public IPathToProjProvider PathToProjProvider { get; }
    public ISolutionPatcherRunner SolutionPatcherRunner { get; }
    public ISolutionPatcherPrep PrepService { get; }
    public IPrintShaIfApplicable PrintShaIfApplicable { get; }
    public string Name => NameProvider.Name;

    public SolutionPatcherRun(
        IPatcherNameProvider nameProvider,
        IPathToProjProvider pathToProjProvider,
        ISolutionPatcherRunner solutionPatcherRunner,
        IPatcherIdProvider idProvider,
        ISolutionPatcherPrep prep,
        IIndexDisseminator indexDisseminator,
        IPrintShaIfApplicable printShaIfApplicable)
    {
        Key = idProvider.InternalId;
        NameProvider = nameProvider;
        PathToProjProvider = pathToProjProvider;
        SolutionPatcherRunner = solutionPatcherRunner;
        PrepService = prep;
        PrintShaIfApplicable = printShaIfApplicable;
        Index = indexDisseminator.GetNext();
    }

    public async Task Prep(CancellationToken cancel)
    {
        await PrepService.Prep(cancel).ConfigureAwait(false);
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