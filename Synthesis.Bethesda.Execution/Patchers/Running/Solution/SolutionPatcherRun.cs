using System.Threading;
using System.Threading.Tasks;
using Synthesis.Bethesda.Execution.CLI;
using Synthesis.Bethesda.Execution.Patchers.Solution;
using Synthesis.Bethesda.Execution.Patchers.TopLevel;

namespace Synthesis.Bethesda.Execution.Patchers.Running.Solution
{
    public interface ISolutionPatcherRun : IPatcherRun
    {
    }

    public class SolutionPatcherRun : ISolutionPatcherRun
    {
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
            ISolutionPatcherPrep prep,
            IExecuteRunnabilityCheck checkRunnability,
            IPrintShaIfApplicable printShaIfApplicable)
        {
            NameProvider = nameProvider;
            PathToProjProvider = pathToProjProvider;
            SolutionPatcherRunner = solutionPatcherRunner;
            PrepService = prep;
            CheckRunnability = checkRunnability;
            PrintShaIfApplicable = printShaIfApplicable;
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
        }

        public override string ToString() => Name;
    }
}
