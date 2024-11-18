using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments.DI;
using Synthesis.Bethesda.CLI.Services.Common;
using Synthesis.Bethesda.Execution.Commands;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.CLI.RunPipeline;


public class ProfileProvider : IProfileProvider, IGameReleaseContext
{
    public Lazy<ISynthesisProfileSettings> Profile { get; }
    
    public ProfileProvider(
        RunPatcherPipelineCommand command,
        PipelineSettingsProvider pipelineSettingsProvider,
        ProfileRetriever profileRetriever)
    {
        Profile = new Lazy<ISynthesisProfileSettings>(() =>
        {
            return profileRetriever.GetProfile(
                pipelineSettingsProvider.Settings.Value.Profiles,
                command.PipelineSettingsPath,
                command.ProfileIdentifier);
        });
    }

    public string ID => Profile.Value.ID;
    public string Name => Profile.Value.Name;
    public GameRelease Release => Profile.Value.Release;
}