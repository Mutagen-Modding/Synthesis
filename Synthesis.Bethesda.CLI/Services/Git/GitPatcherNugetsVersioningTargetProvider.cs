using Synthesis.Bethesda.CLI.Services.Common;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Patchers.Git.Services;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.CLI.Services.Git;

public class GitPatcherNugetsVersioningTargetProvider
{
    private readonly NewestVersionProvider _newestVersionProvider;
    private readonly ProfileVersionProvider _profileVersionProvider;
    private readonly CalculatePatcherVersioning _calculatePatcherVersioning;
    private readonly GithubPatcherSettings _githubPatcherSettings;

    public GitPatcherNugetsVersioningTargetProvider(
        NewestVersionProvider newestVersionProvider,
        ProfileVersionProvider profileVersionProvider,
        CalculatePatcherVersioning calculatePatcherVersioning,
        GithubPatcherSettings githubPatcherSettings)
    {
        _newestVersionProvider = newestVersionProvider;
        _profileVersionProvider = profileVersionProvider;
        _calculatePatcherVersioning = calculatePatcherVersioning;
        _githubPatcherSettings = githubPatcherSettings;
    }

    public async Task<NugetsVersioningTarget> Get(CancellationToken cancel)
    {
        cancel.ThrowIfCancellationRequested();
        var newest = _newestVersionProvider.Latest;
        var profile = _profileVersionProvider.ProfileVersions;
        var active = _calculatePatcherVersioning.Calculate(
            profile: await profile,
            newest: await newest,
            synthManual: _githubPatcherSettings.ManualSynthesisVersion,
            mutaManual: _githubPatcherSettings.ManualMutagenVersion,
            mutaVersioning: _githubPatcherSettings.MutagenVersionType,
            synthVersioning: _githubPatcherSettings.SynthesisVersionType);
        var target = active.TryGetTarget();
        if (target.Failed)
        {
            throw new InvalidOperationException($"No target found for given Github Patcher settings: {_githubPatcherSettings.Nickname}");
        }

        return target.Value;
    }
}