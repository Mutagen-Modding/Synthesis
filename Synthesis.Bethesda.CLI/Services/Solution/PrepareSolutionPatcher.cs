using Synthesis.Bethesda.Execution.Patchers.Running.Solution;

namespace Synthesis.Bethesda.CLI.Services.Solution;

public class PrepareSolutionPatcher
{
    private readonly SolutionPatcherPrep _solutionPatcherPrep;

    public PrepareSolutionPatcher(SolutionPatcherPrep solutionPatcherPrep)
    {
        _solutionPatcherPrep = solutionPatcherPrep;
    }
    
    public async Task Prepare(CancellationToken cancel)
    {
        await _solutionPatcherPrep.Prep(cancel);
    }
}