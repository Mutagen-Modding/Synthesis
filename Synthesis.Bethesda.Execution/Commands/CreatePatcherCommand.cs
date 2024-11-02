﻿using CommandLine;
using Mutagen.Bethesda;
using Noggog;

namespace Synthesis.Bethesda.Execution.Commands;

public class CreatePatcherCommand
{
    [Option('c', "GameCategory",
        HelpText = "Game category that the patcher should be related to",
        Required = true)]
    public GameCategory GameCategory { get; set; }
    
    [Option('d', "ParentDirectory",
        HelpText = "Parent directory to house new solution folder",
        Required = true)]
    public DirectoryPath ParentDirectory { get; set; }
    
    [Option('n', "PatcherName",
        HelpText = "Name to give patcher",
        Required = true)]
    public required string PatcherName { get; set; }

}