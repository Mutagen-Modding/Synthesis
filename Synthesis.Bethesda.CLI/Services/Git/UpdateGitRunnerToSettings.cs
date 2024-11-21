using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Patchers.Git.Services.PrepareRunner;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.CLI.Services.Git;

public class UpdateGitRunnerToSettings
{
    private readonly IPrepareRunnerRepository _prepareRunnerRepository;
    private readonly GithubPatcherSettings _githubPatcherSettings;
    private readonly GitPatcherNugetsVersioningTargetProvider _getNugetVersions;

    public UpdateGitRunnerToSettings(
        IPrepareRunnerRepository prepareRunnerRepository,
        GithubPatcherSettings githubPatcherSettings,
        GitPatcherNugetsVersioningTargetProvider getNugetVersions)
    {
        _prepareRunnerRepository = prepareRunnerRepository;
        _githubPatcherSettings = githubPatcherSettings;
        _getNugetVersions = getNugetVersions;
    }

    public async Task Sync(CancellationToken cancel)
    {
        var versions = await _getNugetVersions.Get(cancel);
        await _prepareRunnerRepository.Checkout(
            new CheckoutInput(
                LibraryNugets: versions,
                Proj: _githubPatcherSettings.SelectedProjectSubpath,
                PatcherVersioning: GitPatcherVersioning.Factory(_githubPatcherSettings)),
            cancel);
    }
}