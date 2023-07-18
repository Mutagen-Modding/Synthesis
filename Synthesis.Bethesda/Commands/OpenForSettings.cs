using CommandLine;
using Mutagen.Bethesda;

namespace Synthesis.Bethesda.Commands;

[Verb("open-for-settings", HelpText = "Informs the patcher to open in settings mode")]
public record OpenForSettings : IBaseRunArgs
{
    [Option('y', "Top", Required = false, HelpText = "Top location to consider when positioning")]
    public int Top { get; set; }

    [Option('x', "Left", Required = false, HelpText = "Left location to consider when positioning")]
    public int Left { get; set; }

    [Option('w', "Width", Required = false, HelpText = "Width consider when positioning")]
    public int Width { get; set; }

    [Option('h', "Height", Required = false, HelpText = "Height to consider when positioning")]
    public int Height { get; set; }

    [Option('g', "GameRelease", Required = false, HelpText = "GameRelease data folder is related to.")]
    public GameRelease? GameRelease { get; set; }
    GameRelease IBaseRunArgs.GameRelease => GameRelease ?? throw new ArgumentNullException(nameof(GameRelease));

    [Option('d', "DataFolderPath", Required = false, HelpText = "Path to the data folder.")]
    public string? DataFolderPath { get; set; }
    string IBaseRunArgs.DataFolderPath => DataFolderPath ?? throw new ArgumentNullException(nameof(DataFolderPath));

    [Option('l', "LoadOrderFilePath", Required = false, HelpText = "Path to the load order file to use.")]
    public string? LoadOrderFilePath { get; set; } = string.Empty;
    string IBaseRunArgs.LoadOrderFilePath => LoadOrderFilePath ?? throw new ArgumentNullException(nameof(LoadOrderFilePath));

    [Option('e', "ExtraDataFolder", Required = false, HelpText = "Path to the user data folder dedicated for a patcher")]
    public string? ExtraDataFolder { get; set; }

    [Option("LoadOrderIncludesCreationClub", Required = false, HelpText = "Whether the load order path file includes CC mods already")]
    public bool LoadOrderIncludesCreationClub { get; set; } = true;

    [Option('i', "InternalDataFolder", Required = false, HelpText = "Path to the internal data folder dedicated for a patcher")]
    public string? InternalDataFolder { get; set; }

    [Option('f', "DefaultDataFolderPath", Required = false, HelpText = "Path to the data folder as the patcher source code defines it.")]
    public string? DefaultDataFolderPath { get; set; }
        
    [Option('k', "ModKey", Required = false, HelpText = "ModKey associated with the patch being generated")]
    public string? ModKey { get; set; }

    public override string ToString()
    {
        return $"{nameof(OpenForSettings)} => \n"
               + $"  {nameof(Top)} => {this.Top} \n"
               + $"  {nameof(Left)} => {this.Left} \n"
               + $"  {nameof(Width)} => {this.Width} \n"
               + $"  {nameof(Height)} => {this.Height} \n"
               + $"  {nameof(ModKey)} => {this.ModKey} \n"
               + $"  {nameof(GameRelease)} => {this.GameRelease} \n"
               + $"  {nameof(DataFolderPath)} => {this.DataFolderPath} \n"
               + $"  {nameof(ExtraDataFolder)} => {this.ExtraDataFolder} \n"
               + $"  {nameof(LoadOrderFilePath)} => {this.LoadOrderFilePath} \n"
               + $"  {nameof(InternalDataFolder)} => {this.InternalDataFolder} \n"
               + $"  {nameof(DefaultDataFolderPath)} => {this.DefaultDataFolderPath} \n"
               + $"  {nameof(LoadOrderIncludesCreationClub)} => {this.LoadOrderIncludesCreationClub}";
    }
}