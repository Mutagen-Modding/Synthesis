using Synthesis.Bethesda.Execution.Patchers.Running.Git;

namespace Synthesis.Bethesda.CLI.Services.Git;

public class PrepareGitPatcher
{
    private readonly IGitPatcherPrep _gitPatcherPrep;
    private readonly UpdateGitRunnerToSettings _updateGitRunnerToSettings;

    public PrepareGitPatcher(
        IGitPatcherPrep gitPatcherPrep,
        UpdateGitRunnerToSettings updateGitRunnerToSettings)
    {
        _gitPatcherPrep = gitPatcherPrep;
        _updateGitRunnerToSettings = updateGitRunnerToSettings;
    }
    
    public async Task Prepare(CancellationToken cancel)
    {
        await _gitPatcherPrep.Prep(cancel);
        await _updateGitRunnerToSettings.Sync(cancel);
    }
}