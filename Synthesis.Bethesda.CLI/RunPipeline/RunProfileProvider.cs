using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments.DI;
using Noggog;
using Synthesis.Bethesda.Execution.Profile;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.CLI.RunPipeline;

public interface IRunProfileProvider
{
    ISynthesisProfileSettings Get();
}

public class RunProfileProvider : IRunProfileProvider, IGameReleaseContext
{
    public IProfileNameProvider ProfileNameProvider { get; }
    public IPipelineProfilesProvider PipelineProfilesProvider { get; }

    private Lazy<ISynthesisProfileSettings> _profileSettings;

    public RunProfileProvider(
        IProfileNameProvider profileNameProvider,
        IPipelineProfilesProvider pipelineProfilesProvider)
    {
        ProfileNameProvider = profileNameProvider;
        PipelineProfilesProvider = pipelineProfilesProvider;
        _profileSettings = new Lazy<ISynthesisProfileSettings>(GetInternal);
    }

    private ISynthesisProfileSettings GetInternal()
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

    public ISynthesisProfileSettings Get() => _profileSettings.Value;
    public GameRelease Release => _profileSettings.Value.TargetRelease;

    public override string ToString()
    {
        return nameof(RunProfileProvider);
    }
}