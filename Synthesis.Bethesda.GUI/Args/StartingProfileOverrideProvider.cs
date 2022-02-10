using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.GUI.Settings;

namespace Synthesis.Bethesda.GUI.Args;

public class StartingProfileOverrideProvider
{
    private readonly ArgClassProvider _argClassProvider;

    public StartingProfileOverrideProvider(
        ArgClassProvider argClassProvider)
    {
        _argClassProvider = argClassProvider;
    }
    
    public string Get(ISynthesisGuiSettings settings, IPipelineSettings pipelineSettings)
    {
        if (_argClassProvider.GetArgObject() is not StartCommand start) return settings.SelectedProfile;
        foreach (var profile in pipelineSettings.Profiles)
        {
            if (profile.Nickname == start.Profile) return profile.ID;
        }

        return start.Profile ?? settings.SelectedProfile;
    }
}