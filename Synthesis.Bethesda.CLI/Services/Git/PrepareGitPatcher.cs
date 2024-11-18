namespace Synthesis.Bethesda.CLI.Services.Git;

public class PrepareGitPatcher
{
    private readonly UpdateGitRunnerToSettings _updateGitRunnerToSettings;

    public PrepareGitPatcher(
        UpdateGitRunnerToSettings updateGitRunnerToSettings)
    {
        _updateGitRunnerToSettings = updateGitRunnerToSettings;
    }
    
    public async Task Prepare(CancellationToken cancel)
    {
        await _updateGitRunnerToSettings.Sync(cancel);
    }
}