using CommandLine;
using Mutagen.Bethesda;

namespace Synthesis.Bethesda.Commands;

[Verb("open-for-settings", HelpText = "Informs the patcher to open in settings mode")]
public record OpenForSettings
{
    [Option('y', "Top", Required = false, HelpText = "Top location to consider when positioning")]
    public int Top { get; set; }

    [Option('x', "Left", Required = false, HelpText = "Left location to consider when positioning")]
    public int Left { get; set; }

    [Option('w', "Width", Required = false, HelpText = "Width consider when positioning")]
    public int Width { get; set; }

    [Option('h', "Height", Required = false, HelpText = "Height to consider when positioning")]
    public int Height { get; set; }

    [Option('g', "GameRelease", Required = true, HelpText = "GameRelease data folder is related to.")]
    public GameRelease GameRelease { get; set; }

    [Option('d', "DataFolderPath", Required = true, HelpText = "Path to the data folder.")]
    public string DataFolderPath { get; set; } = string.Empty;

    [Option('l', "LoadOrderFilePath", Required = false, HelpText = "Path to the load order file to use.")]
    public string LoadOrderFilePath { get; set; } = string.Empty;

    public override string ToString()
    {
        return $"{nameof(OpenForSettings)} => \n"
               + $"  {nameof(Top)} => {this.Top} \n"
               + $"  {nameof(Left)} => {this.Left} \n"
               + $"  {nameof(Width)} => {this.Width} \n"
               + $"  {nameof(Height)} => {this.Height} \n"
               + $"  {nameof(GameRelease)} => {this.GameRelease} \n"
               + $"  {nameof(DataFolderPath)} => {this.DataFolderPath} \n"
               + $"  {nameof(LoadOrderFilePath)} => {this.LoadOrderFilePath}";
    }
}