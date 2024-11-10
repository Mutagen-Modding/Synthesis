using System.IO.Abstractions;
using Autofac;
using Mutagen.Bethesda.Environments.DI;
using Mutagen.Bethesda.Synthesis.Profiles;
using Synthesis.Bethesda.CLI.Common;
using Synthesis.Bethesda.Execution.Commands;
using Synthesis.Bethesda.Execution.Settings.V2;

namespace Synthesis.Bethesda.CLI.CreateProfileCli;

public class CreateProfileRunner
{
    private readonly CreateProfileId _createProfileId;
    private readonly PipelineSettingsModifier _pipelineSettingsModifier;

    public CreateProfileRunner(
        CreateProfileId createProfileId,
        PipelineSettingsModifier pipelineSettingsModifier)
    {
        _createProfileId = createProfileId;
        _pipelineSettingsModifier = pipelineSettingsModifier;
    }

    internal void RunInternal(CreateProfileCommand cmd)
    {
        _pipelineSettingsModifier.DoModification(cmd.SettingsFolderPath, (pipelineSettings) =>
        {
            var existingProfileIds = pipelineSettings.Profiles.Select(x => x.ID).ToHashSet();
            pipelineSettings.Profiles.Add(new SynthesisProfile()
            {
                TargetRelease = cmd.GameRelease,
                ID = _createProfileId.GetNewProfileId(existingProfileIds),
                Groups = new List<PatcherGroupSettings>()
                {
                    new PatcherGroupSettings()
                    {
                        On = true,
                        Name = cmd.InitialGroupName
                    }
                },
                Nickname = cmd.ProfileName,
            });
        });
    }
    
    public static async Task<int> Run(CreateProfileCommand cmd)
    {
        try
        {
            var b = new ContainerBuilder();
            b.RegisterModule(new CreateProfileModule(new FileSystem()));
            var cont = b.Build();
            var create = cont.Resolve<CreateProfileRunner>();
            create.RunInternal(cmd);
        }
        catch (Exception ex)
        {
            System.Console.Error.WriteLine(ex);
            return -1;
        }
        return 0;
    }
}