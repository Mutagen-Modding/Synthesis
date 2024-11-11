using Noggog;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.CLI.Common;

public class ProfileRetriever
{
    public ISynthesisProfileSettings GetProfile(
        IReadOnlyCollection<ISynthesisProfileSettings> profiles,
        string settingsPath,
        string? profileIdentifier)
    {
        if (profileIdentifier.IsNullOrEmpty())
        {
            var profile = profiles.FirstOrDefault();
            if (profile == null)
            {
                throw new ArgumentException($"There were no profiles to run in settings path {settingsPath}");
            }

            return profile;
        }
        else
        {
            var profile = profiles.FirstOrDefault(x => x.Nickname == profileIdentifier);
            if (profile != null)
            {
                return profile;
            }
            profile = profiles.FirstOrDefault(x => x.ID == profileIdentifier);
            if (profile == null)
            {
                throw new KeyNotFoundException($"Could not find a profile {profileIdentifier} in settings path {settingsPath}");
            }
            
            return profile;
        }
    }
}