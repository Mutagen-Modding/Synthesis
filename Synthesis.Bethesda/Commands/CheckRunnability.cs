using CommandLine;
using Mutagen.Bethesda;

namespace Synthesis.Bethesda.Commands;

[Verb("check-runnability", HelpText = "Check the current state and see if the patcher thinks it can run")]
public record CheckRunnability : IBaseRunArgs
{
    [Option('g', "GameRelease", Required = true, HelpText = "GameRelease data folder is related to.")]
    public GameRelease GameRelease { get; set; }

    [Option('d', "DataFolderPath", Required = true, HelpText = "Path to the data folder.")]
    public string DataFolderPath { get; set; } = string.Empty;

    [Option('l', "LoadOrderFilePath", Required = false, HelpText = "Path to the load order file to use.")]
    public string LoadOrderFilePath { get; set; } = string.Empty;

    [Option('e', "ExtraDataFolder", Required = false, HelpText = "Path to the user data folder dedicated for a patcher")]
    public string? ExtraDataFolder { get; set; }

    [Option("LoadOrderIncludesCreationClub", Required = false, HelpText = "Whether the load order path file includes CC mods already")]
    public bool LoadOrderIncludesCreationClub { get; set; } = true;
        
    [Option('k', "ModKey", Required = false, HelpText = "ModKey associated with the patch being generated")]
    public string? ModKey { get; set; }

    [Option('i', "InternalDataFolder", Required = false, HelpText = "Path to the internal data folder dedicated for a patcher")]
    public string? InternalDataFolder { get; set; }

    [Option('f', "DefaultDataFolderPath", Required = false, HelpText = "Path to the data folder as the patcher source code defines it.")]
    public string? DefaultDataFolderPath { get; set; }

    public override string ToString()
    {
        return $"{nameof(CheckRunnability)} => \n"
               + $"  {nameof(GameRelease)} => {this.GameRelease} \n"
               + $"  {nameof(DataFolderPath)} => {this.DataFolderPath} \n"
               + $"  {nameof(LoadOrderFilePath)} => {this.LoadOrderFilePath} \n"
               + $"  {nameof(ExtraDataFolder)} => {this.ExtraDataFolder} \n"
               + $"  {nameof(ModKey)} => {this.ModKey} \n"
               + $"  {nameof(InternalDataFolder)} => {this.InternalDataFolder} \n"
               + $"  {nameof(DefaultDataFolderPath)} => {this.DefaultDataFolderPath} \n"
               + $"  {nameof(LoadOrderIncludesCreationClub)} => {this.LoadOrderIncludesCreationClub}";
    }
}