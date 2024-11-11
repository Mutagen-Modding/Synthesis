using CommandLine;
using Mutagen.Bethesda;
using Noggog;

namespace Synthesis.Bethesda.Execution.Commands;

[Verb("create-template-patcher", HelpText = "Create a new patcher project template")]
public class CreateTemplatePatcherCommand
{
    [Option('c', "GameCategory",
        HelpText = "Game category that the patcher should be related to",
        Required = true)]
    public required GameCategory GameCategory { get; set; }
    
    [Option('d', "ParentDirectory",
        HelpText = "Parent directory to house new solution folder",
        Required = true)]
    public required string ParentDirectory { get; set; }
    
    [Option('n', "PatcherName",
        HelpText = "Name to give patcher",
        Required = true)]
    public required string PatcherName { get; set; }
}
