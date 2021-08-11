using System;
using System.Linq;
using Noggog;
using Synthesis.Bethesda.Execution.Profile;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.Execution.Running.Cli
{
    public interface IRunProfileProvider
    {
        ISynthesisProfileSettings Get();
    }

    public class RunProfileProvider : IRunProfileProvider
    {
        public IProfileNameProvider ProfileNameProvider { get; }
        public IPipelineProfilesProvider PipelineProfilesProvider { get; }

        public RunProfileProvider(
            IProfileNameProvider profileNameProvider,
            IPipelineProfilesProvider pipelineProfilesProvider)
        {
            ProfileNameProvider = profileNameProvider;
            PipelineProfilesProvider = pipelineProfilesProvider;
        }

        public ISynthesisProfileSettings Get()
        {
            var profiles = PipelineProfilesProvider.Get().ToArray();

            var targetName = ProfileNameProvider.Name;

            ISynthesisProfileSettings? profile = null;
            if (!targetName.IsNullOrWhitespace())
            {
                profile = profiles.FirstOrDefault(profile =>
                {
                    if (targetName.Equals(profile.Nickname)) return true;
                    if (targetName.Equals(profile.ID)) return true;
                    return false;
                });
            }
            else if (profiles.Length == 1)
            {
                profile = profiles[0];
            }

            if (profile == null)
            {
                throw new ArgumentException("File and target name did not point to a valid profile");
            }

            if (string.IsNullOrWhiteSpace(profile.ID))
            {
                throw new ArgumentException("Profile had empty ID");
            }

            return profile;
        }
    }
}