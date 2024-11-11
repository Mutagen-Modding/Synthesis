using CommandLine;
using Mutagen.Bethesda;
using Noggog;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.Execution.Commands;

[Verb("create-profile", HelpText = "Create a new profile")]
public class CreateProfileCommand : ISettingsFolderProvider
{
    [Option('r', "GameRelease",
        HelpText = "Game release that the profile should be related to",
        Required = true)]
    public GameRelease GameRelease { get; set; }
    
    [Option('n', "ProfileName",
        HelpText = "Name to give profile",
        Required = true)]
    public required string ProfileName { get; set; }
    
    [Option('s', "SettingsFolderPath",
        HelpText = "Path to the folder containing the PipelineSettings.json to be adjusted",
        Required = true)]
    public required string SettingsFolderPath { get; set; }
    
    [Option('g', "InitialGroupName",
        HelpText = "Name to give the initial patcher group",
        Required = true)]
    public required string InitialGroupName { get; set; }

    DirectoryPath ISettingsFolderProvider.SettingsFolder => SettingsFolderPath;
}
