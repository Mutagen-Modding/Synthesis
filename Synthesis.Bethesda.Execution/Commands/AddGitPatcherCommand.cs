﻿using CommandLine;

namespace Synthesis.Bethesda.Execution.Commands;

[Verb("add-git-patcher", HelpText = "Adds a git patcher to a profile")]
public class AddGitPatcherCommand
{
    [Option('n', "ProfileName",
        HelpText = "Name of profile to add to",
        Required = true)]
    public required string ProfileName { get; set; }
    
    [Option('p', "SettingsFolderPath",
        HelpText = "Path to the folder containing the PipelineSettings.json to be adjusted",
        Required = true)]
    public required string SettingsFolderPath { get; set; }
    
    [Option('g', "GroupName",
        HelpText = "Name of the patcher group to add patcher to",
        Required = true)]
    public required string GroupName { get; set; }
    
    [Option('a', "GitRepoAddress",
        HelpText = "Address to the repository to add the git patcher from",
        Required = true)]
    public required string GitRepoAddress { get; set; }
    
    [Option("SelectedProjectSubpath",
        HelpText = "Project subpath to target.  Need to check the source code and find the target .csproj within.",
        Required = true)]
    public required string SelectedProjectSubpath { get; set; }
    
    [Option("PatcherNickname",
        HelpText = "Nickname to give the patcher",
        Required = false)]
    public required string? Nickname { get; set; }
}