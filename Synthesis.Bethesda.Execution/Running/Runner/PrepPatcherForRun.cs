using System;
using System.Threading;
using System.Threading.Tasks;
using Synthesis.Bethesda.Execution.Patchers.Running;
using Synthesis.Bethesda.Execution.Reporters;
using Synthesis.Bethesda.Execution.WorkEngine;

namespace Synthesis.Bethesda.Execution.Running.Runner;

public interface IPrepPatcherForRun
{
    PatcherPrepBundle Prep(IPatcherRun patcher, CancellationToken cancellation);
}

public class PrepPatcherForRun : IPrepPatcherForRun
{
    private readonly IWorkDropoff _workDropoff;
    public IRunReporter Reporter { get; }
        
    public PrepPatcherForRun(
        IWorkDropoff workDropoff,
        IRunReporter reporter)
    {
        _workDropoff = workDropoff;
        Reporter = reporter;
    }
        
    public PatcherPrepBundle Prep(IPatcherRun patcher, CancellationToken cancellation)
    {
        return new PatcherPrepBundle(
            patcher,
            _workDropoff.EnqueueAndWait(async () =>
            {
                try
                {
                    await patcher.Prep(cancellation).ConfigureAwait(false);
                }
                catch (TaskCanceledException)
                {
                }
                catch (Exception ex)
                {
                    Reporter.ReportPrepProblem(patcher.Key, patcher.Name, ex);
                    return ex;
                }

                return default(Exception?);
            }));
    }
}