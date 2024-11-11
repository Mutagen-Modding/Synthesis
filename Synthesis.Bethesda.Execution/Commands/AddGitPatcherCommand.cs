using CommandLine;
using Noggog;
using Synthesis.Bethesda.Execution.Pathing;

namespace Synthesis.Bethesda.Execution.Commands;

[Verb("add-git-patcher", HelpText = "Adds a git patcher to a profile")]
public class AddGitPatcherCommand : IPipelineSettingsPath
{
    [Option('p', "ProfileIdentifier",
        HelpText = "Nickname/GUID of profile to add to",
        Required = true)]
    public required string ProfileIdentifier { get; set; }

    [Option('s', "PipelineSettingsPath",
        HelpText = "Path to a specific pipeline settings to read from",
        Required = true)]
    public string PipelineSettingsPath { get; set; } = string.Empty;
    
    [Option('g', "GroupName",
        HelpText = "Name of the patcher group to add patcher to",
        Required = true)]
    public required string GroupName { get; set; }
    
    [Option("PatcherNickname",
        HelpText = "Nickname to give the patcher",
        Required = false)]
    public required string? Nickname { get; set; }
    
    [Option('a', "GitRepoAddress",
        HelpText = "Address to the repository to add the git patcher from",
        Required = true)]
    public required string GitRepoAddress { get; set; }
    
    [Option("ProjectSubpath",
        HelpText = "Project subpath to target",
        Required = true)]
    public required string ProjectSubpath { get; set; }

    FilePath IPipelineSettingsPath.Path => PipelineSettingsPath;
}