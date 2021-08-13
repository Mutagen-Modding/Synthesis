using System;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using Synthesis.Bethesda.Execution.PatcherCommands;
using Synthesis.Bethesda.Execution.Patchers.Common;
using Synthesis.Bethesda.Execution.Patchers.Solution;
using Synthesis.Bethesda.Execution.Running.Cli;

namespace Synthesis.Bethesda.Execution.Patchers.Running.Solution
{
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
        public IExecuteRunnabilityCheck CheckRunnability { get; }
        public IPrintShaIfApplicable PrintShaIfApplicable { get; }
        public string Name => NameProvider.Name;

        public SolutionPatcherRun(
            IPatcherNameProvider nameProvider,
            IPathToProjProvider pathToProjProvider,
            ISolutionPatcherRunner solutionPatcherRunner,
            IPatcherIdProvider idProvider,
            ISolutionPatcherPrep prep,
            IIndexDisseminator indexDisseminator,
            IExecuteRunnabilityCheck checkRunnability,
            IPrintShaIfApplicable printShaIfApplicable)
        {
            Key = idProvider.InternalId;
            NameProvider = nameProvider;
            PathToProjProvider = pathToProjProvider;
            SolutionPatcherRunner = solutionPatcherRunner;
            PrepService = prep;
            CheckRunnability = checkRunnability;
            PrintShaIfApplicable = printShaIfApplicable;
            Index = indexDisseminator.GetNext();
        }

        public Task Prep(CancellationToken cancel)
        {
            return PrepService.Prep(cancel);
        }

        public async Task Run(RunSynthesisPatcher settings, CancellationToken cancel)
        {
            PrintShaIfApplicable.Print();
            
            var runnability = await CheckRunnability.Check(
                PathToProjProvider.Path,
                directExe: false,
                loadOrderPath: settings.LoadOrderFilePath,
                cancel: cancel);

            if (runnability.Failed)
            {
                throw new CliUnsuccessfulRunException((int)Codes.NotRunnable, runnability.Reason);
            }
            
            await SolutionPatcherRunner.Run(settings, cancel);
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
}
