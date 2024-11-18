using Noggog;
using Synthesis.Bethesda.Execution.Versioning;
using Synthesis.Bethesda.Execution.Versioning.Query;

namespace Synthesis.Bethesda.CLI.Services.Common;

public class NewestVersionProvider
{
    private AsyncLazy<NugetVersionPair> _latestVersion;
    public Task<NugetVersionPair> Latest => _latestVersion.Value;

    public NewestVersionProvider(
        IQueryNewestLibraryVersions queryNewest,
        IProfileProvider profileSettings)
    {
        _latestVersion = new AsyncLazy<NugetVersionPair>(async () =>
        {
            var latest = await queryNewest.GetLatestVersions(CancellationToken.None);
            var prerelease = profileSettings.Profile.Value.ConsiderPrereleaseNugets;
            return new NugetVersionPair(
                Mutagen: prerelease ? latest.Prerelease.Mutagen : latest.Normal.Mutagen,
                Synthesis: prerelease ? latest.Prerelease.Synthesis : latest.Normal.Synthesis);
        });
    }
}