using Noggog;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Patchers.Git.Services;

namespace Synthesis.Bethesda.CLI.Services.Common;

public class ProfileVersionProvider
{
    private AsyncLazy<ActiveNugetVersioning> _profileVersions;
    public Task<ActiveNugetVersioning> ProfileVersions => _profileVersions.Value;

    public ProfileVersionProvider(
        NewestVersionProvider newestVersionProvider,
        CalculateProfileVersioning calculateProfileVersioning,
        IProfileProvider profileProvider)
    {
        _profileVersions = new AsyncLazy<ActiveNugetVersioning>(async () =>
        {
            var profileSettings = profileProvider.Profile.Value;
            var newest = await newestVersionProvider.Latest;
            var profileVersions = calculateProfileVersioning.Calculate(
                profileSettings.MutagenVersioning,
                profileSettings.MutagenManualVersion,
                newest.Mutagen,
                profileSettings.SynthesisVersioning,
                profileSettings.SynthesisManualVersion,
                newest.Synthesis);
            return profileVersions;
        });
    }
}