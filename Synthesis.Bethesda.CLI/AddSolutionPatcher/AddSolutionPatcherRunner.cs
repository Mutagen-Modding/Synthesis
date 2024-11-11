using System.IO.Abstractions;
using Autofac;
using Synthesis.Bethesda.CLI.Common;
using Synthesis.Bethesda.Execution.Commands;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.CLI.AddSolutionPatcher;

public class AddSolutionPatcherRunner
{
    private readonly PipelineSettingsModifier _pipelineSettingsModifier;

    public AddSolutionPatcherRunner(
        PipelineSettingsModifier pipelineSettingsModifier)
    {
        _pipelineSettingsModifier = pipelineSettingsModifier;
    }
    
    public async Task Add(AddSolutionPatcherCommand cmd)
    {
        await _pipelineSettingsModifier.DoModification(cmd.SettingsFolderPath, async (pipelineSettings, settingsPath) =>
        {
            var profile = pipelineSettings.Profiles.FirstOrDefault(x => x.Nickname == cmd.ProfileName);
            if (profile == null)
            {
                throw new KeyNotFoundException($"Could not find a profile name {cmd.ProfileName} in settings path {settingsPath}");
            }
            
            var group = profile.Groups.FirstOrDefault(x => x.Name == cmd.GroupName);
            if (group == null)
            {
                throw new KeyNotFoundException($"Could not find a group name {cmd.GroupName} within profile {cmd.ProfileName} in settings path {settingsPath}");
            }
            
            var settings = new SolutionPatcherSettings()
            {
                Nickname = cmd.Nickname ?? string.Empty,
                On = true,
                SolutionPath = cmd.SolutionPath,
                ProjectSubpath = cmd.ProjectSubpath,
            };
            group.Patchers.Add(settings);
        });
    }
    
    public static async Task<int> Run(AddSolutionPatcherCommand cmd)
    {
        try
        {
            var b = new ContainerBuilder();
            b.RegisterModule(new AddSolutionPatcherModule(new FileSystem()));
            var cont = b.Build();
            var adder = cont.Resolve<AddSolutionPatcherRunner>();
            await adder.Add(cmd);
        }
        catch (Exception ex)
        {
            System.Console.Error.WriteLine(ex);
            return -1;
        }
        return 0;
    }
}