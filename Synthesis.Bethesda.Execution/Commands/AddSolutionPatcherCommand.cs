using CommandLine;

namespace Synthesis.Bethesda.Execution.Commands;

[Verb("add-solution-patcher", HelpText = "Adds a solution patcher to a profile")]
public class AddSolutionPatcherCommand
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
    
    [Option("PatcherNickname",
        HelpText = "Nickname to give the patcher",
        Required = false)]
    public required string? Nickname { get; set; }
    
    [Option('s', "SolutionPath",
        HelpText = "Address to the solution to add the patcher from",
        Required = true)]
    public required string SolutionPath { get; set; }
    
    [Option("ProjectSubpath",
        HelpText = "Project subpath to target",
        Required = true)]
    public required string ProjectSubpath { get; set; }
}