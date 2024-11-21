using CommandLine;
using Mutagen.Bethesda;
using Noggog;
using Synthesis.Bethesda.Execution.Pathing;

namespace Synthesis.Bethesda.Execution.Commands;

[Verb("create-profile", HelpText = "Create a new profile")]
public class CreateProfileCommand : IPipelineSettingsPath
{
    [Option('r', "GameRelease",
        HelpText = "Game release that the profile should be related to",
        Required = true)]
    public GameRelease GameRelease { get; set; }
    
    [Option('n', "ProfileName",
        HelpText = "Name to give profile",
        Required = true)]
    public required string ProfileName { get; set; }

    [Option('s', "PipelineSettingsPath",
        HelpText = "Path to a specific pipeline settings to read from",
        Required = true)]
    public string PipelineSettingsPath { get; set; } = string.Empty;
    
    [Option('g', "InitialGroupName",
        HelpText = "Name to give the initial patcher group",
        Required = true)]
    public required string InitialGroupName { get; set; }

    FilePath IPipelineSettingsPath.Path => PipelineSettingsPath;
}
